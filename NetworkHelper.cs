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
        private static TcpClient? currentClient;
        private static readonly byte[] buffer = new byte[2097152]; // Buffer of 2MB for faster transfers

        // Optimized method to send a file with reduced overhead
        public static void SendFile(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
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
                    client.NoDelay = true;
                    client.ReceiveBufferSize = buffer.Length;
                    client.SendBufferSize = buffer.Length;
                    client.Connect(ipAddress, 5001);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Send file size and name before actual data
                        stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);

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
                                    MessageBox.Show("Transfer canceled.");
                                    stream.Close();
                                    client.Close();
                                    ResetProgressBar(progressBar, lblSpeed);
                                    return;
                                }

                                stream.Write(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));

                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds;
                                    lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                }
                            }
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
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"I/O error: {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Error reading the file.");
                }
                ResetProgressBar(progressBar, lblSpeed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unknown error: {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show("Error sending the file.");
                }
                ResetProgressBar(progressBar, lblSpeed);
            }
        }

        // Starts the receiving server with optimized buffer management
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => ReceiveFile(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest;
                serverThread.Start();
                MessageBox.Show("Receiving server started.");
            }
        }

        // Stops the server and ensures all connections are closed cleanly
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                listener.Stop();

                if (currentClient != null && currentClient.Connected)
                {
                    currentClient.Close();
                    currentClient = null;
                }

                listener = null;
                MessageBox.Show("The server has been stopped.");
            }
        }

        // Optimized file receiving with larger buffer sizes and faster transfer
        public static void ReceiveFile(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    currentClient = client;
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileSizeBytes = new byte[8];

                        int bytesRead = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);

                        if (bytesRead == 0)
                        {
                            continue;
                        }

                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0');

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

                                progressBar.Invoke((MethodInvoker)(() => progressBar.Maximum = (int)(fileSize / 1024)));

                                while (totalBytesReceived < fileSize)
                                {
                                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                                    if (System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Contains("CANCELLED"))
                                    {
                                        MessageBox.Show("Transfer canceled by the sender. You may need to restart the server.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        return;
                                    }
                                    if (!client.Connected)
                                    {
                                        fs.Close();
                                        File.Delete(savePath);
                                        MessageBox.Show("Transfer canceled.");
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
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                    }
                                }
                            }

                            MessageBox.Show($"File received: {savePath}");
                            ResetProgressBar(progressBar, lblSpeed);
                        }
                    }
                    client.Close();
                    currentClient = null;
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
