using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace UTransfer
{
    public static class NetworkHelper
    {
        private static TcpListener? listener;
        private static bool isServerRunning = false;
        private static Thread? serverThread;
        private static readonly byte[] buffer = new byte[1048576]; // Buffer optimisé de 1 Mo

        // Démarre le serveur de réception avec un buffer optimisé
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => StartServer(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest; // Performance maximale
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
                listener = null;
                MessageBox.Show("Le serveur a été arrêté.");
            }
        }

        // Démarre le serveur et gère les connexions entrantes
        private static void StartServer(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => ReceiveFiles(client, progressBar, lblSpeed));
                    clientThread.IsBackground = true;
                    clientThread.Start();
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
                Debug.WriteLine($"Erreur lors du démarrage du serveur : {ex.Message}");
            }
        }

        // Méthode pour lire exactement 'size' octets du flux
        private static void ReadExact(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < size)
            {
                int bytesRead = stream.Read(buffer, offset + totalBytesRead, size - totalBytesRead);
                if (bytesRead == 0)
                {
                    throw new IOException("Fin inattendue du flux");
                }
                totalBytesRead += bytesRead;
            }
        }

        // Méthode pour recevoir plusieurs fichiers avec un buffer optimisé
        private static void ReceiveFiles(TcpClient client, ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Lecture du nombre de fichiers
                    byte[] fileCountBytes = new byte[4];
                    ReadExact(stream, fileCountBytes, 0, 4);
                    int fileCount = BitConverter.ToInt32(fileCountBytes, 0);

                    // Lecture des noms de fichiers et des tailles
                    List<string> fileNames = new List<string>();
                    List<long> fileSizes = new List<long>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        // Lecture de la longueur du nom de fichier
                        byte[] fileNameLengthBytes = new byte[4];
                        ReadExact(stream, fileNameLengthBytes, 0, 4);
                        int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                        // Lecture du nom de fichier
                        byte[] fileNameBytes = new byte[fileNameLength];
                        ReadExact(stream, fileNameBytes, 0, fileNameLength);
                        string fileName = Encoding.UTF8.GetString(fileNameBytes);

                        // Lecture de la taille du fichier
                        byte[] fileSizeBytes = new byte[8];
                        ReadExact(stream, fileSizeBytes, 0, 8);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        fileNames.Add(fileName);
                        fileSizes.Add(fileSize);
                    }

                    // Affichage d'une boîte de dialogue pour accepter tous les fichiers
                    string filesList = string.Join("\n", fileNames);
                    if (MessageBox.Show($"Voulez-vous recevoir les fichiers suivants ?\n{filesList}", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fichiers_recus");
                        Directory.CreateDirectory(saveFolderPath);

                        List<string> receivedFiles = new List<string>();

                        for (int i = 0; i < fileCount; i++)
                        {
                            string fileName = fileNames[i];
                            long fileSize = fileSizes[i];
                            string savePath = Path.Combine(saveFolderPath, fileName);

                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                long totalBytesReceived = 0;
                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                progressBar.Invoke((MethodInvoker)(() =>
                                {
                                    progressBar.Value = 0;
                                    progressBar.Maximum = (int)Math.Max(fileSize, 1); // Assure que le maximum est au moins 1
                                }));

                                while (totalBytesReceived < fileSize)
                                {
                                    int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesReceived);
                                    int bytesRead = stream.Read(buffer, 0, bytesToRead);

                                    if (bytesRead == 0)
                                    {
                                        // Si aucun octet n'est lu avant d'avoir reçu la taille attendue du fichier, c'est une annulation
                                        if (totalBytesReceived < fileSize)
                                        {
                                            fs.Close();
                                            File.Delete(savePath);
                                            MessageBox.Show($"Le transfert du fichier '{fileName}' a été annulé par l'envoyeur.");
                                            ResetProgressBar(progressBar, lblSpeed);
                                            break;
                                        }
                                        else
                                        {
                                            // Nous avons terminé de lire le fichier
                                            break;
                                        }
                                    }

                                    fs.Write(buffer, 0, bytesRead);
                                    totalBytesReceived += bytesRead;

                                    // Mise à jour de la barre de progression et de la vitesse
                                    long bytesReceivedCopy = totalBytesReceived;
                                    progressBar.Invoke((MethodInvoker)(() =>
                                    {
                                        progressBar.Value = (int)Math.Min(bytesReceivedCopy, progressBar.Maximum);
                                    }));
                                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                    if (elapsedSeconds > 0)
                                    {
                                        double speed = (bytesReceivedCopy / 1024.0 / 1024.0) / elapsedSeconds;
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                    }
                                }

                                // Vérifie si le transfert s'est terminé normalement
                                if (totalBytesReceived == fileSize)
                                {
                                    receivedFiles.Add(savePath);
                                }
                                else
                                {
                                    // Si le fichier n'a pas été entièrement reçu, il a été annulé
                                    fs.Close();
                                    File.Delete(savePath);
                                    // Le message d'annulation a déjà été affiché dans la boucle
                                    break; // Sortir de la boucle si un transfert a été annulé
                                }

                                ResetProgressBar(progressBar, lblSpeed);
                            }
                        }

                        // Affiche un message récapitulatif des fichiers reçus
                        if (receivedFiles.Count > 0)
                        {
                            string receivedFilesList = string.Join("\n", receivedFiles);
                            MessageBox.Show($"Fichiers reçus :\n{receivedFilesList}");
                        }
                        else
                        {
                            MessageBox.Show("Aucun fichier n'a été reçu.");
                        }
                    }
                }
                client.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la réception des fichiers : {ex.Message}");
                MessageBox.Show($"Erreur lors de la réception des fichiers : {ex.Message}");
            }
        }

        // Méthode pour envoyer plusieurs fichiers séquentiellement avec envoi des métadonnées au début
        public static void SendFiles(string ipAddress, List<string> filePaths, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            Thread sendThread = new Thread(() =>
            {
                try
                {
                    Debug.WriteLine($"Tentative de connexion à {ipAddress} pour envoyer des fichiers.");
                    using (TcpClient client = new TcpClient())
                    {
                        client.NoDelay = true;  // Désactive l'algorithme de Nagle pour de meilleures performances
                        client.ReceiveBufferSize = buffer.Length;
                        client.SendBufferSize = buffer.Length;
                        client.Connect(ipAddress, 5001);

                        using (NetworkStream stream = client.GetStream())
                        {
                            // Envoie le nombre de fichiers
                            int fileCount = filePaths.Count;
                            byte[] fileCountBytes = BitConverter.GetBytes(fileCount);
                            stream.Write(fileCountBytes, 0, fileCountBytes.Length);

                            // Envoie les noms de fichiers et leurs tailles
                            foreach (string filePath in filePaths)
                            {
                                if (!File.Exists(filePath))
                                {
                                    MessageBox.Show($"Le fichier {filePath} n'existe pas.");
                                    continue;
                                }

                                string fileName = Path.GetFileName(filePath);
                                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                                int fileNameLength = fileNameBytes.Length;
                                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);
                                long fileSize = new FileInfo(filePath).Length;
                                byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                                // Envoie la longueur du nom, le nom et la taille du fichier
                                stream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                                stream.Write(fileNameBytes, 0, fileNameBytes.Length);
                                stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                            }

                            List<string> sentFiles = new List<string>();

                            foreach (string filePath in filePaths)
                            {
                                if (isCancelled())
                                {
                                    MessageBox.Show("Envoi annulé.");
                                    break;
                                }

                                if (!File.Exists(filePath))
                                {
                                    continue; // Le fichier n'existe pas, passer au suivant
                                }

                                string fileName = Path.GetFileName(filePath);
                                long fileSize = new FileInfo(filePath).Length;

                                using (FileStream fs = File.OpenRead(filePath))
                                {
                                    int bytesRead;
                                    long totalBytesSent = 0;
                                    Stopwatch stopwatch = new Stopwatch();
                                    stopwatch.Start();

                                    // Mise à jour de la barre de progression avant l'envoi
                                    progressBar.Invoke((MethodInvoker)(() =>
                                    {
                                        progressBar.Value = 0;
                                        progressBar.Maximum = (int)Math.Max(fileSize, 1); // Assure que le maximum est au moins 1
                                    }));

                                    // Envoie du fichier
                                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        if (isCancelled())
                                        {
                                            // Notifie l'annulation et ferme la connexion
                                            MessageBox.Show("Envoi annulé.");
                                            stream.Close();  // Ferme le flux correctement
                                            client.Close();  // Ferme la connexion client
                                            ResetProgressBar(progressBar, lblSpeed);
                                            return;
                                        }

                                        try
                                        {
                                            stream.Write(buffer, 0, bytesRead);
                                        }
                                        catch (IOException ex)
                                        {
                                            // Vérifie si l'exception est due à une fermeture de connexion par le receveur
                                            if (ex.InnerException is SocketException socketEx &&
                                                (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.Shutdown))
                                            {
                                                MessageBox.Show("Le transfert a été annulé par le receveur.");
                                            }
                                            else
                                            {
                                                MessageBox.Show("Envoi annulé.");
                                            }
                                            ResetProgressBar(progressBar, lblSpeed);
                                            return;
                                        }

                                        totalBytesSent += bytesRead;

                                        // Mise à jour de la barre de progression et de la vitesse
                                        long bytesSentCopy = totalBytesSent;
                                        progressBar.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBar.Value = (int)Math.Min(bytesSentCopy, progressBar.Maximum);
                                        }));
                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                        if (elapsedSeconds > 0)
                                        {
                                            double speed = (bytesSentCopy / 1024.0 / 1024.0) / elapsedSeconds; // Vitesse en MB/s
                                            lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                        }
                                    }

                                    sentFiles.Add(filePath);
                                    ResetProgressBar(progressBar, lblSpeed);
                                }
                            }

                            // Affiche un message récapitulatif des fichiers envoyés
                            if (sentFiles.Count > 0)
                            {
                                string sentFilesList = string.Join("\n", sentFiles);
                                MessageBox.Show($"Fichiers envoyés avec succès :\n{sentFilesList}");
                            }
                            else
                            {
                                MessageBox.Show("Aucun fichier n'a été envoyé.");
                            }
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
                        MessageBox.Show($"Erreur lors de l'envoi des fichiers : {ex.Message}");
                    }
                    ResetProgressBar(progressBar, lblSpeed);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur inconnue : {ex.Message}");
                    if (!isCancelled())
                    {
                        MessageBox.Show($"Erreur lors de l'envoi des fichiers : {ex.Message}");
                    }
                    ResetProgressBar(progressBar, lblSpeed);
                }
            });
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        // Réinitialise la barre de progression et l'affichage de la vitesse
        private static void ResetProgressBar(ProgressBar progressBar, Label lblSpeed)
        {
            progressBar.Invoke((MethodInvoker)(() =>
            {
                progressBar.Value = 0;
                progressBar.Maximum = 100; // Valeur par défaut pour éviter les problèmes
                lblSpeed.Text = "Vitesse : 0 MB/s";
            }));
        }
    }
}
