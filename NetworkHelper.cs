using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public static class NetworkHelper
    {
        private static TcpListener? listener;
        private static bool isServerRunning = false;
        private static Thread? serverThread;
        private static bool isTransferCancelled = false;
        private const int BufferSize = 65536; // 64 Ko

        // Méthode pour envoyer un fichier à une adresse IP spécifiée avec une ProgressBar et un Label de vitesse
        public static async void SendFile(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Le fichier sélectionné n'existe pas.");
                    return;
                }

                Debug.WriteLine($"Tentative de connexion à {ipAddress} pour envoyer le fichier {filePath}.");

                using (TcpClient client = new TcpClient())
                {
                    client.NoDelay = true;  // Désactive l'algorithme de Nagle pour une meilleure performance
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); // Active KeepAlive
                    await client.ConnectAsync(ipAddress, 5001);  // Connexion au serveur

                    using (NetworkStream networkStream = client.GetStream())
                    {
                        // Envoyer les métadonnées (taille et nom du fichier)
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        await networkStream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length);
                        byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                        await networkStream.WriteAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                        await networkStream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                        // Envoyer le fichier
                        byte[] buffer = new byte[BufferSize];
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                        {
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            progressBar.Invoke((MethodInvoker)(() =>
                            {
                                progressBar.Maximum = 100;
                                progressBar.Value = 0;
                            }));

                            int bytesRead;
                            double lastUpdateTime = 0;

                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                if (isCancelled())
                                {
                                    Debug.WriteLine("Transfert annulé.");
                                    // Envoyer un message de contrôle d'annulation
                                    byte[] controlMessage = System.Text.Encoding.UTF8.GetBytes("<CANCEL>");
                                    await networkStream.WriteAsync(controlMessage, 0, controlMessage.Length);
                                    MessageBox.Show("Transfert annulé.");
                                    break;
                                }

                                await networkStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds - lastUpdateTime >= 0.5) // Mettre à jour toutes les 500 ms
                                {
                                    double progress = (double)totalBytesSent / fileSize * 100;
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // en MB/s

                                    progressBar.Invoke((MethodInvoker)(() =>
                                    {
                                        progressBar.Value = (int)progress;
                                    }));

                                    lblSpeed.Invoke((MethodInvoker)(() =>
                                    {
                                        lblSpeed.Text = $"Vitesse : {speed:F2} MB/s";
                                    }));

                                    lastUpdateTime = elapsedSeconds;
                                }
                            }
                        }

                        if (!isCancelled())
                        {
                            MessageBox.Show("Fichier envoyé avec succès.");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Erreur de socket : {ex.Message}");
                MessageBox.Show("Erreur de connexion au serveur. Vérifiez que le serveur est en ligne.", "Erreur de connexion");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Erreur d'entrée/sortie : {ex.Message}");
                MessageBox.Show("Erreur lors de la lecture du fichier ou de l'écriture sur le réseau.", "Erreur de fichier");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur inconnue lors de l'envoi : {ex.Message}");
                MessageBox.Show("Une erreur s'est produite lors de l'envoi du fichier : " + ex.Message, "Erreur");
            }
        }

        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isTransferCancelled = false;
                isServerRunning = true;
                serverThread = new Thread(() => ReceiveFile(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Start();
                MessageBox.Show("Serveur de réception démarré avec succès.");
            }
        }

        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                isTransferCancelled = true; // Arrête les transferts en cours
                listener.Stop();
                listener = null;
                Debug.WriteLine("Le serveur a été arrêté.");
                MessageBox.Show("Le serveur a été arrêté.");
            }
        }

        public static void ReceiveFile(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();
                Debug.WriteLine("Serveur en attente de connexion...");

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Task.Run(async () =>
                    {
                        using (NetworkStream networkStream = client.GetStream())
                        {
                            // Recevoir les métadonnées (taille et nom du fichier)
                            byte[] fileSizeBytes = new byte[8];
                            await networkStream.ReadAsync(fileSizeBytes, 0, fileSizeBytes.Length);
                            long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                            byte[] fileNameLengthBytes = new byte[4];
                            await networkStream.ReadAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                            byte[] fileNameBytes = new byte[fileNameLength];
                            await networkStream.ReadAsync(fileNameBytes, 0, fileNameBytes.Length);
                            string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes);

                            if (MessageBox.Show($"Recevoir le fichier {fileName} ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fichiers_recus");

                                if (!Directory.Exists(saveFolderPath))
                                {
                                    Directory.CreateDirectory(saveFolderPath);
                                }

                                string savePath = Path.Combine(saveFolderPath, fileName);
                                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
                                {
                                    byte[] buffer = new byte[BufferSize];
                                    long totalBytesReceived = 0;
                                    Stopwatch stopwatch = new Stopwatch();
                                    stopwatch.Start();

                                    progressBar.Invoke((MethodInvoker)(() =>
                                    {
                                        progressBar.Maximum = 100;
                                        progressBar.Value = 0;
                                    }));

                                    int bytesRead;
                                    double lastUpdateTime = 0;

                                    while (totalBytesReceived < fileSize)
                                    {
                                        bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                                        if (bytesRead == 0)
                                        {
                                            break; // Fin du flux
                                        }

                                        // Vérifier les messages de contrôle
                                        string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                        if (message.Contains("<CANCEL>"))
                                        {
                                            fs.Close();
                                            File.Delete(savePath);  // Supprime le fichier partiellement reçu
                                            MessageBox.Show("Le transfert a été annulé par l'envoyeur.");
                                            return;
                                        }

                                        await fs.WriteAsync(buffer, 0, bytesRead);
                                        totalBytesReceived += bytesRead;

                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                        if (elapsedSeconds - lastUpdateTime >= 0.5) // Mettre à jour toutes les 500 ms
                                        {
                                            double progress = (double)totalBytesReceived / fileSize * 100;
                                            double speed = (totalBytesReceived / 1024.0 / 1024.0) / elapsedSeconds; // en MB/s

                                            progressBar.Invoke((MethodInvoker)(() =>
                                            {
                                                progressBar.Value = (int)progress;
                                            }));

                                            lblSpeed.Invoke((MethodInvoker)(() =>
                                            {
                                                lblSpeed.Text = $"Vitesse : {speed:F2} MB/s";
                                            }));

                                            lastUpdateTime = elapsedSeconds;
                                        }

                                        if (isTransferCancelled)
                                        {
                                            MessageBox.Show("Transfert annulé.");
                                            break;
                                        }
                                    }
                                }

                                if (!isTransferCancelled)
                                    MessageBox.Show($"Fichier reçu : {savePath}");
                            }
                        }
                        client.Close();
                    });
                }
            }
            catch (SocketException ex)
            {
                if (!isServerRunning)
                {
                    Debug.WriteLine("Le serveur a été arrêté proprement.");
                }
                else
                {
                    Debug.WriteLine($"Erreur de socket : {ex.Message}");
                    MessageBox.Show("Erreur lors de la réception du fichier : " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la réception : {ex.Message}");
                MessageBox.Show("Erreur lors de la réception : " + ex.Message);
            }
        }
    }
}
