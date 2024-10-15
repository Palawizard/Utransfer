using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        private const int BufferSize = 65536; // Augmenté à 64 Ko

        // Méthode pour envoyer un fichier à une adresse IP spécifiée avec une ProgressBar et un Label de vitesse
        public static void SendFile(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
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
                    client.Connect(ipAddress, 5001);  // Connexion au serveur

                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 5000;  // Définit un délai d'attente pour éviter les blocages
                        stream.WriteTimeout = 5000;

                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);

                        byte[] buffer = new byte[BufferSize]; // Utilisation d'un buffer de 128 Ko
                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            int bytesRead;
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (isCancelled())
                                {
                                    Debug.WriteLine("Transfert annulé.");
                                    byte[] cancelMessage = System.Text.Encoding.UTF8.GetBytes("CANCELLED");
                                    stream.Write(cancelMessage, 0, cancelMessage.Length);
                                    MessageBox.Show("Transfert annulé.");
                                    break;
                                }

                                stream.Write(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));

                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // en MB/s
                                    lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
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
                MessageBox.Show("Erreur lors de la lecture du fichier. Assurez-vous que le fichier est accessible.", "Erreur de fichier");
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
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 5000; // Définit un délai d'attente pour éviter les blocages
                        stream.WriteTimeout = 5000;

                        byte[] fileSizeBytes = new byte[8];
                        stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0');

                        if (MessageBox.Show($"Recevoir le fichier {fileName} ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fichiers_recus");

                            if (!Directory.Exists(saveFolderPath))
                            {
                                Directory.CreateDirectory(saveFolderPath);
                            }

                            string savePath = Path.Combine(saveFolderPath, fileName);
                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buffer = new byte[BufferSize]; // Utilisation d'un buffer de 128 Ko
                                long totalBytesReceived = 0;

                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                                while (totalBytesReceived < fileSize)
                                {
                                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                    string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

                                    // Si un message d'annulation est reçu, supprimer le fichier
                                    if (message.Contains("CANCELLED"))
                                    {
                                        fs.Close();
                                        File.Delete(savePath);  // Supprime le fichier partiellement reçu
                                        MessageBox.Show("Le transfert a été annulé par l'envoyeur.");
                                        return;
                                    }

                                    if (bytesRead == 0 || isTransferCancelled)
                                    {
                                        if (isTransferCancelled) MessageBox.Show("Transfert annulé.");
                                        break;
                                    }
                                    fs.Write(buffer, 0, bytesRead);
                                    totalBytesReceived += bytesRead;

                                    progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesReceived / 1024)));

                                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                    if (elapsedSeconds > 0)
                                    {
                                        double speed = (totalBytesReceived / 1024.0 / 1024.0) / elapsedSeconds;
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                    }
                                }
                            }

                            if (!isTransferCancelled) MessageBox.Show($"Fichier reçu : {savePath}");
                        }
                    }
                    client.Close();
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
