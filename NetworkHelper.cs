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
        private static TcpClient? currentClient; // Référence au client en cours pour fermeture
        private static readonly byte[] buffer = new byte[1048576]; // Buffer optimisé de 1 Mo

        // Méthode pour envoyer un fichier avec un buffer optimisé
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
                    client.ReceiveBufferSize = buffer.Length; // Optimise la taille du buffer
                    client.SendBufferSize = buffer.Length;
                    client.Connect(ipAddress, 5001);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Envoie la taille et le nom du fichier
                        stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);

                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            int bytesRead;
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            // Mise à jour de la barre de progression avant l'envoi
                            progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                            // Optimisation du transfert en utilisant un buffer plus large
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (isCancelled())
                                {
                                    // Envoie un message d'annulation au receveur
                                    byte[] cancelMessage = System.Text.Encoding.UTF8.GetBytes("CANCELLED");
                                    stream.Write(cancelMessage, 0, cancelMessage.Length);

                                    // Notifie l'annulation
                                    MessageBox.Show("Transfert annulé.");
                                    stream.Close();  // Ferme la connexion proprement
                                    client.Close();  // Ferme la connexion client
                                    ResetProgressBar(progressBar, lblSpeed);
                                    return;
                                }

                                stream.Write(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                // Mise à jour de la barre de progression et de la vitesse
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));
                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // Vitesse en MB/s
                                    lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                }
                            }
                        }

                        MessageBox.Show("Fichier envoyé avec succès.");
                        ResetProgressBar(progressBar, lblSpeed);
                    }
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Erreur de socket : {ex.Message}");
                MessageBox.Show("Erreur de connexion au serveur. Vérifiez que le serveur est en ligne.");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Erreur d'E/S : {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Erreur lors de la lecture du fichier.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur inconnue : {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Erreur lors de l'envoi du fichier.");
                }
            }
        }

        // Démarre le serveur de réception avec un buffer optimisé
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => ReceiveFile(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest; // Priorité maximale
                serverThread.Start();
                MessageBox.Show("Serveur de réception démarré.");
            }
        }

        // Arrête le serveur et ferme toutes les connexions proprement
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                listener.Stop();

                // Si une connexion est active, la fermer
                if (currentClient != null && currentClient.Connected)
                {
                    currentClient.Close();
                    currentClient = null;  // Réinitialise la référence
                }

                listener = null;
                MessageBox.Show("Le serveur a été arrêté.");
            }
        }

        // Méthode pour recevoir un fichier avec un buffer optimisé
        public static void ReceiveFile(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    currentClient = client; // Stocke la connexion active
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileSizeBytes = new byte[8];

                        // Attente des données du client
                        int bytesRead = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                        if (bytesRead == 0) continue; // Aucun fichier reçu

                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0').Replace("\0", ""); // Supprime les caractères nuls

                        if (MessageBox.Show($"Recevoir le fichier {fileName} ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fichiers_recus");
                            Directory.CreateDirectory(saveFolderPath);
                            string savePath = Path.Combine(saveFolderPath, fileName);

                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                long totalBytesReceived = 0;
                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                                while (totalBytesReceived < fileSize)
                                {
                                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                                    // Vérifie si l'envoyeur a annulé le transfert
                                    string receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    if (receivedData.Contains("CANCELLED"))
                                    {
                                        fs.Close();
                                        File.Delete(savePath);
                                        MessageBox.Show("Le transfert a été annulé par l'envoyeur.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        return;
                                    }

                                    if (!client.Connected || bytesRead == 0)
                                    {
                                        fs.Close();
                                        File.Delete(savePath);
                                        MessageBox.Show("Transfert annulé.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        return;
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

                            MessageBox.Show($"Fichier reçu : {savePath}");
                            ResetProgressBar(progressBar, lblSpeed);
                        }
                    }
                    client.Close();
                    currentClient = null;  // Réinitialise la référence
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Erreur de socket : {ex.Message}");
                if (isServerRunning)
                {
                    MessageBox.Show("Transfert annulé.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la réception : {ex.Message}");
                if (isServerRunning)
                {
                    MessageBox.Show("Transfert annulé.");
                }
            }
        }

        // Réinitialise la barre de progression et l'affichage de la vitesse
        private static void ResetProgressBar(ProgressBar progressBar, Label lblSpeed)
        {
            progressBar.Invoke((MethodInvoker)(() =>
            {
                progressBar.Value = 0;
                lblSpeed.Text = "Vitesse : 0 MB/s";
            }));
        }
    }
}
