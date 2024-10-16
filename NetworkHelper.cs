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
        private static TcpClient? currentClient; // Reference to the current client to force closure when stopping the server
        private static byte[] buffer = new byte[524288]; // 512 KB for faster transfer

        // Method to send a file to a specified IP address
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
                    client.NoDelay = true;  // Disable Nagle's algorithm for better performance
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, buffer.Length);
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, buffer.Length);

                    client.Connect(ipAddress, 5001);  // Connect to the server

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Send the file size and name
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
                                    byte[] cancelMessage = System.Text.Encoding.UTF8.GetBytes("CANCELLED");
                                    stream.Write(cancelMessage, 0, cancelMessage.Length);
                                    MessageBox.Show("Transfer canceled.");
                                    ResetProgressBar(progressBar, lblSpeed);
                                    return;
                                }

                                stream.Write(buffer, 0, bytesRead);
                                totalBytesSent += bytesRead;

                                // Update progress bar and speed
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));
                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // MB/s
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
                MessageBox.Show("Error reading the file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unknown error: {ex.Message}");
                MessageBox.Show("Error sending the file.");
            }
        }

        // Starts the receiving server
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => ReceiveFile(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest; // Max priority to minimize interruptions
                serverThread.Start();
                MessageBox.Show("Receiving server started.");
            }
        }

        // Stops the server
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                listener.Stop();

                // If a transfer is in progress, close the connection
                if (currentClient != null && currentClient.Connected)
                {
                    currentClient.Close();
                    currentClient = null;  // Reset the client reference
                }

                listener = null;
                MessageBox.Show("The server has been stopped.");
            }
        }

        // Method to receive a file
        public static void ReceiveFile(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    currentClient = client; // Store the active connection
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileSizeBytes = new byte[8];
                        stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        byte[] fileNameBytes = new byte[1024];
                        stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes).TrimEnd('\0').Replace("\0", ""); // Remove null characters

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
                                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                    if (System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Contains("CANCELLED") || !client.Connected)
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
                    currentClient = null;  // Reset the reference after the transfer
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Socket error: {ex.Message}");
                MessageBox.Show("Error receiving the file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Receiving error: {ex.Message}");
                MessageBox.Show("Error receiving the file.");
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
