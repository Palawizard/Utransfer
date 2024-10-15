using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace UTransfer
{
    public static class NetworkHelper
    {
        private static TcpListener? listener;
        private static bool isServerRunning = false;
        private static Thread? serverThread;

        // Méthode pour envoyer un fichier à une adresse IP spécifiée avec une ProgressBar et un Label de vitesse
        public static void SendFile(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed)
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
                    client.Connect(ipAddress, 5001); // Connexion au serveur

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Envoyer le nom du fichier encodé pour éviter les caractères corrompus
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Envoyer la taille et le nom du fichier
                        stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);

                        byte[] buffer = new byte[1024];
                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            int bytesRead;
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            // Initialiser la barre de progression
                            progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024))); // Taille en ko

                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                stream.Write(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                // Mise à jour de la barre de progression
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));

                                // Calculer la vitesse en MB/s
                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // en MB/s
                                    lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                }
                            }
                        }

                        MessageBox.Show("Fichier envoyé avec succès.");
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

        // Méthode pour démarrer le serveur dans un thread séparé avec ProgressBar et Label de vitesse
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => ReceiveFile(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Start();
                MessageBox.Show("Serveur de réception démarré avec succès.");
            }
        }

        // Méthode pour arrêter le serveur
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                listener.Stop();
                listener = null;
                Debug.WriteLine("Le serveur a été arrêté.");
                MessageBox.Show("Le serveur a été arrêté.");
            }
        }

        // Méthode pour recevoir un fichier avec une ProgressBar et un Label de vitesse
        public static void ReceiveFile(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();
                Debug.WriteLine("Serveur en attente de connexion...");

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient(); // Accepte une connexion client
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileSizeBytes = new byte[8];
                        stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0');

                        if (MessageBox.Show($"Recevoir le fichier {fileName} ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fichiers_recus");

                            // Vérifier et créer le dossier "fichiers_recus" s'il n'existe pas
                            if (!Directory.Exists(saveFolderPath))
                            {
                                Directory.CreateDirectory(saveFolderPath);
                            }

                            string savePath = Path.Combine(saveFolderPath, fileName);
                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buffer = new byte[1024];
                                long totalBytesReceived = 0;

                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                // Initialiser la barre de progression
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                                while (totalBytesReceived < fileSize)
                                {
                                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                    if (bytesRead == 0)
                                    {
                                        break;
                                    }
                                    fs.Write(buffer, 0, bytesRead);
                                    totalBytesReceived += bytesRead;

                                    // Mise à jour de la barre de progression
                                    progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesReceived / 1024)));

                                    // Calculer la vitesse en MB/s
                                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                    if (elapsedSeconds > 0)
                                    {
                                        double speed = (totalBytesReceived / 1024.0 / 1024.0) / elapsedSeconds; // en MB/s
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Vitesse : {speed:F2} MB/s"));
                                    }
                                }
                            }

                            MessageBox.Show($"Fichier reçu : {savePath}");
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
