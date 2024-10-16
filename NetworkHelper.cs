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
        private static CancellationTokenSource? cancellationTokenSource;
        private static TcpClient? currentClient; // Référence au client actuel pour forcer la fermeture lors de l'arrêt du serveur
        private static readonly byte[] buffer = new byte[1048576]; // Buffer de 1 Mo

        // Méthode optimisée pour envoyer un fichier avec des opérations asynchrones
        public static async Task SendFileAsync(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("The selected file does not exist.");
                    return;
                }

                Debug.WriteLine($"Attempting to connect to {ipAddress} to send the file {filePath}.");

                using (TcpClient client = new TcpClient())
                {
                    client.NoDelay = true;  // Désactive l'algorithme de Nagle pour une transmission plus rapide des petits paquets
                    client.ReceiveBufferSize = buffer.Length; // Optimise la taille du buffer pour le socket
                    client.SendBufferSize = buffer.Length;
                    await client.ConnectAsync(ipAddress, 5001);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Envoie la taille et le nom du fichier avant les données réelles
                        await stream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length);
                        await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            int bytesRead;
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            progressBar.Invoke((MethodInvoker)(() =>
                            {
                                progressBar.Maximum = (int)(fileSize / 1024);
                                progressBar.Value = 0;
                            }));

                            long lastUpdate = 0;

                            // Optimise la lecture du fichier et l'écriture réseau en utilisant des opérations asynchrones
                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                if (isCancelled())
                                {
                                    // Envoie une notification d'annulation au récepteur
                                    byte[] cancelMessage = System.Text.Encoding.UTF8.GetBytes("CANCELLED");
                                    await stream.WriteAsync(cancelMessage, 0, cancelMessage.Length);

                                    // Affiche le message d'annulation et nettoie
                                    MessageBox.Show("Transfer canceled.");
                                    ResetProgressBar(progressBar, lblSpeed);  // Réinitialise la barre de progression
                                    return;
                                }

                                await stream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                // Met à jour la barre de progression et la vitesse à intervalles plus longs (toutes les 500 ms)
                                if (stopwatch.ElapsedMilliseconds - lastUpdate >= 500)
                                {
                                    lastUpdate = stopwatch.ElapsedMilliseconds;

                                    progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));
                                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                    if (elapsedSeconds > 0)
                                    {
                                        double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // Vitesse en Mo/s
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                    }
                                }
                            }

                            // Mise à jour finale de la barre de progression et de la vitesse
                            progressBar.Invoke((MethodInvoker)(() => progressBar.Value = progressBar.Maximum));
                            double finalSpeed = (totalBytesSent / 1024.0 / 1024.0) / stopwatch.Elapsed.TotalSeconds;
                            lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {finalSpeed:F2} MB/s"));
                        }

                        MessageBox.Show("File sent successfully.");
                        ResetProgressBar(progressBar, lblSpeed);
                    }
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Socket error: {ex.Message}");
                MessageBox.Show("Error connecting to the server. Ensure the server is online.");
                ResetProgressBar(progressBar, lblSpeed);  // Réinitialise la barre de progression en cas d'erreur
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"I/O error: {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Error reading the file.");
                }
                ResetProgressBar(progressBar, lblSpeed);  // Réinitialise la barre de progression en cas d'erreur
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unknown error: {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Error sending the file.");
                }
                ResetProgressBar(progressBar, lblSpeed);  // Réinitialise la barre de progression en cas d'erreur
            }
        }

        // Démarre le serveur de réception avec une gestion optimisée du buffer
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ReceiveFileAsync(progressBar, lblSpeed, cancellationTokenSource.Token), cancellationTokenSource.Token);
                MessageBox.Show("Receiving server started.");
            }
        }

        // Arrête le serveur et s'assure que toutes les connexions sont fermées proprement
        public static void StopServer()
        {
            if (isServerRunning && cancellationTokenSource != null)
            {
                isServerRunning = false;
                cancellationTokenSource.Cancel();

                // Ferme la connexion active s'il y en a une
                if (currentClient != null && currentClient.Connected)
                {
                    currentClient.Close();
                    currentClient = null;  // Réinitialise la référence du client
                }

                listener?.Stop();
                listener = null;
                MessageBox.Show("The server has been stopped.");
            }
        }

        // Réception de fichier optimisée avec des tailles de buffer plus grandes et une meilleure gestion des connexions
        public static async Task ReceiveFileAsync(ProgressBar progressBar, Label lblSpeed, CancellationToken cancellationToken)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    currentClient = client; // Stocke la connexion active

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileSizeBytes = new byte[8];

                        // Attend les données du client
                        int bytesRead = await stream.ReadAsync(fileSizeBytes, 0, fileSizeBytes.Length);

                        // Passe à l'itération suivante si aucune donnée n'est reçue
                        if (bytesRead == 0)
                        {
                            continue;
                        }

                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        await stream.ReadAsync(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0').Replace("\0", ""); // Supprime les caractères nuls

                        if (MessageBox.Show($"Receive the file {fileName}?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "received_files");
                            Directory.CreateDirectory(saveFolderPath);
                            string savePath = Path.Combine(saveFolderPath, fileName);

                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                long totalBytesReceived = 0;
                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                progressBar.Invoke((MethodInvoker)(() =>
                                {
                                    progressBar.Maximum = (int)(fileSize / 1024);
                                    progressBar.Value = 0;
                                }));

                                long lastUpdate = 0;

                                while (totalBytesReceived < fileSize)
                                {
                                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                    if (bytesRead == 0)
                                    {
                                        // Si le flux est fermé, le transfert est annulé
                                        fs.Close();
                                        File.Delete(savePath);
                                        MessageBox.Show("Transfer canceled.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        return;
                                    }

                                    string receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    if (receivedData.Contains("CANCELLED"))
                                    {
                                        // Informe le récepteur que le transfert a été annulé et suggère de redémarrer le serveur
                                        MessageBox.Show("Transfer canceled by the sender. You may need to restart the server.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        return;
                                    }

                                    await fs.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesReceived += bytesRead;

                                    // Met à jour la barre de progression et la vitesse à intervalles plus longs (toutes les 500 ms)
                                    if (stopwatch.ElapsedMilliseconds - lastUpdate >= 500)
                                    {
                                        lastUpdate = stopwatch.ElapsedMilliseconds;

                                        progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesReceived / 1024)));
                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                        if (elapsedSeconds > 0)
                                        {
                                            double speed = (totalBytesReceived / 1024.0 / 1024.0) / elapsedSeconds;
                                            lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                        }
                                    }
                                }

                                // Mise à jour finale de la barre de progression et de la vitesse
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = progressBar.Maximum));
                                double finalSpeed = (totalBytesReceived / 1024.0 / 1024.0) / stopwatch.Elapsed.TotalSeconds;
                                lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {finalSpeed:F2} MB/s"));
                            }

                            MessageBox.Show($"File received: {savePath}");
                            ResetProgressBar(progressBar, lblSpeed);
                        }
                    }
                    client.Close();
                    currentClient = null;  // Réinitialise la référence après le transfert
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Socket error: {ex.Message}");
                if (isServerRunning)
                {
                    MessageBox.Show("Transfer canceled.");
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal du serveur
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Receiving error: {ex.Message}");
                if (isServerRunning)
                {
                    MessageBox.Show("Transfer canceled.");
                }
            }
        }

        private static void ResetProgressBar(ProgressBar progressBar, Label lblSpeed)
        {
            progressBar.Invoke((MethodInvoker)(() =>
            {
                progressBar.Value = 0;
                lblSpeed.Text = "Speed: 0 MB/s";
            }));
        }
    }
}
