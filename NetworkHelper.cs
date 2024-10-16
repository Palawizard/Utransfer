using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

namespace UTransfer
{
    public static class NetworkHelper
    {
        private static TcpListener? listener;
        private static bool isServerRunning = false;
        private static Thread? serverThread;
        private static readonly byte[] buffer = new byte[1048576]; // Optimized 1 MB buffer

        // Starts the receiving server with an optimized buffer
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverThread = new Thread(() => StartServer(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest; // Maximize performance
                serverThread.Start();
                MessageBox.Show("Receiving server started.");
            }
        }

        // Stops the server and closes all connections properly
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                listener.Stop();
                listener = null;
                MessageBox.Show("The server has been stopped.");
            }
        }

        // Starts the server and handles incoming connections
        private static void StartServer(ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5001);
                listener.Start();

                while (isServerRunning)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => ReceiveFile(client, progressBar, lblSpeed));
                    clientThread.IsBackground = true;
                    clientThread.Start();
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
                Debug.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        // Method to receive a file with an optimized buffer
        private static void ReceiveFile(TcpClient client, ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] fileSizeBytes = new byte[8];

                    // Wait for data from the client
                    int bytesRead = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                    if (bytesRead == 0)
                    {
                        client.Close();
                        return; // No file received
                    }

                    long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                    byte[] fileNameLengthBytes = new byte[4];
                    stream.Read(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    byte[] fileNameBytes = new byte[fileNameLength];
                    stream.Read(fileNameBytes, 0, fileNameLength);
                    string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes);

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
                                progressBar.Value = 0;
                                progressBar.Maximum = (int)(fileSize / 1024);
                            }));

                            while (totalBytesReceived < fileSize)
                            {
                                bytesRead = stream.Read(buffer, 0, buffer.Length);

                                if (bytesRead == 0)
                                {
                                    // If no bytes are read, the connection was closed (transfer canceled)
                                    fs.Close();
                                    File.Delete(savePath);
                                    MessageBox.Show("The transfer was canceled by the sender.");
                                    ResetProgressBar(progressBar, lblSpeed);
                                    break;
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

                            // Check if the transfer completed successfully
                            if (totalBytesReceived == fileSize)
                            {
                                MessageBox.Show($"File received: {savePath}");
                            }
                            ResetProgressBar(progressBar, lblSpeed);
                        }
                    }
                }
                client.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving file: {ex.Message}");
                MessageBox.Show("Transfer canceled.");
            }
        }

        // Method to send multiple files sequentially
        public static void SendFiles(string ipAddress, List<string> filePaths, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            Thread sendThread = new Thread(() =>
            {
                foreach (string filePath in filePaths)
                {
                    if (isCancelled())
                    {
                        MessageBox.Show("Sending canceled.");
                        break;
                    }

                    SendFile(ipAddress, filePath, progressBar, lblSpeed, isCancelled);
                }
            });
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        // Method to send a single file
        private static void SendFile(string ipAddress, string filePath, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"The file {filePath} does not exist.");
                    return;
                }

                Debug.WriteLine($"Attempting to connect to {ipAddress} to send the file {filePath}.");

                using (TcpClient client = new TcpClient())
                {
                    client.NoDelay = true;  // Disable Nagle's algorithm for better performance
                    client.ReceiveBufferSize = buffer.Length; // Optimize buffer size
                    client.SendBufferSize = buffer.Length;
                    client.Connect(ipAddress, 5001);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                        int fileNameLength = fileNameBytes.Length;
                        byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

                        long fileSize = new FileInfo(filePath).Length;
                        byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                        // Send the file size, name length, and name
                        stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                        stream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);

                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            int bytesRead;
                            long totalBytesSent = 0;
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();

                            // Update the progress bar before sending
                            progressBar.Invoke((MethodInvoker)(() =>
                            {
                                progressBar.Value = 0;
                                progressBar.Maximum = (int)(fileSize / 1024);
                            }));

                            // Optimize the transfer using a larger buffer
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (isCancelled())
                                {
                                    // Notify cancellation and close the connection
                                    MessageBox.Show($"Transfer of {fileName} canceled.");
                                    stream.Close();  // Close the stream properly
                                    client.Close();  // Close the client connection
                                    ResetProgressBar(progressBar, lblSpeed);
                                    return;
                                }

                                try
                                {
                                    stream.Write(buffer, 0, bytesRead);
                                }
                                catch (IOException ex)
                                {
                                    // Check if the exception is due to the receiver closing the connection
                                    if (ex.InnerException is SocketException socketEx &&
                                        (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.Shutdown))
                                    {
                                        MessageBox.Show($"The transfer of {fileName} was canceled by the receiver.");
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Transfer of {fileName} canceled.");
                                    }
                                    ResetProgressBar(progressBar, lblSpeed);
                                    return;
                                }

                                totalBytesSent += bytesRead;

                                // Update progress bar and speed
                                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = (int)(totalBytesSent / 1024)));
                                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                if (elapsedSeconds > 0)
                                {
                                    double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // Speed in MB/s
                                    lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                }
                            }
                        }

                        MessageBox.Show($"File {filePath} sent successfully.");
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
                    // Check if the exception is due to the receiver closing the connection
                    if (ex.InnerException is SocketException socketEx &&
                        (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.Shutdown))
                    {
                        MessageBox.Show($"The transfer was canceled by the receiver.");
                    }
                    else
                    {
                        MessageBox.Show($"Error sending the file {filePath}.");
                    }
                }
                ResetProgressBar(progressBar, lblSpeed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unknown error: {ex.Message}");
                if (!isCancelled())
                {
                    MessageBox.Show($"Error sending the file {filePath}.");
                }
                ResetProgressBar(progressBar, lblSpeed);
            }
        }

        // Resets the progress bar and speed display
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
