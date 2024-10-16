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
        private static readonly byte[] buffer = new byte[1048576]; // Optimized 1 MB buffer
        private static List<TcpClient> connectedClients = new List<TcpClient>(); // Track connected clients

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

                // Close the listener to unblock AcceptTcpClient
                listener.Stop();

                // Close all connected clients
                lock (connectedClients)
                {
                    foreach (var client in connectedClients)
                    {
                        client.Close();
                    }
                    connectedClients.Clear();
                }

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
                    TcpClient client;
                    try
                    {
                        client = listener.AcceptTcpClient();
                    }
                    catch (SocketException ex)
                    {
                        // If the listener is stopped, AcceptTcpClient will throw an exception
                        if (!isServerRunning)
                        {
                            break; // Exit the loop if the server is stopped
                        }
                        else
                        {
                            Debug.WriteLine($"Socket error: {ex.Message}");
                            continue;
                        }
                    }

                    lock (connectedClients)
                    {
                        connectedClients.Add(client);
                    }

                    Thread clientThread = new Thread(() => ReceiveFiles(client, progressBar, lblSpeed));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        // Helper method to read exactly 'size' bytes from the stream
        private static void ReadExact(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < size)
            {
                int bytesRead = stream.Read(buffer, offset + totalBytesRead, size - totalBytesRead);
                if (bytesRead == 0)
                {
                    throw new IOException("Unexpected end of stream");
                }
                totalBytesRead += bytesRead;
            }
        }

        // Method to receive multiple files with an optimized buffer
        private static void ReceiveFiles(TcpClient client, ProgressBar progressBar, Label lblSpeed)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Read the number of files
                    byte[] fileCountBytes = new byte[4];
                    ReadExact(stream, fileCountBytes, 0, 4);
                    int fileCount = BitConverter.ToInt32(fileCountBytes, 0);

                    // Read the filenames and sizes
                    List<string> fileNames = new List<string>();
                    List<long> fileSizes = new List<long>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        // Read filename length
                        byte[] fileNameLengthBytes = new byte[4];
                        ReadExact(stream, fileNameLengthBytes, 0, 4);
                        int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                        // Read filename
                        byte[] fileNameBytes = new byte[fileNameLength];
                        ReadExact(stream, fileNameBytes, 0, fileNameLength);
                        string fileName = Encoding.UTF8.GetString(fileNameBytes);

                        // Read file size
                        byte[] fileSizeBytes = new byte[8];
                        ReadExact(stream, fileSizeBytes, 0, 8);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        fileNames.Add(fileName);
                        fileSizes.Add(fileSize);
                    }

                    // Display a single confirmation dialog for all files
                    string filesList = string.Join("\n", fileNames);
                    if (MessageBox.Show($"Do you want to receive the following files?\n{filesList}", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        string saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "received_files");
                        Directory.CreateDirectory(saveFolderPath);

                        List<string> receivedFiles = new List<string>();

                        for (int i = 0; i < fileCount; i++)
                        {
                            if (!isServerRunning)
                            {
                                // Server stopped, cancel the transfer
                                break;
                            }

                            string fileName = fileNames[i];
                            long fileSize = fileSizes[i];

                            // Get a unique file path to prevent overwriting existing files
                            string savePath = GetUniqueFilePath(saveFolderPath, fileName);

                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                long totalBytesReceived = 0;
                                Stopwatch stopwatch = new Stopwatch();
                                stopwatch.Start();

                                progressBar.Invoke((MethodInvoker)(() =>
                                {
                                    progressBar.Value = 0;
                                    progressBar.Maximum = (int)Math.Max(fileSize, 1); // Ensure maximum is at least 1
                                }));

                                while (totalBytesReceived < fileSize)
                                {
                                    if (!isServerRunning)
                                    {
                                        // Server stopped, cancel the transfer
                                        fs.Close();
                                        File.Delete(savePath);
                                        MessageBox.Show($"Transfer of file '{fileName}' was canceled by the receiver.");
                                        ResetProgressBar(progressBar, lblSpeed);
                                        break;
                                    }

                                    int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesReceived);
                                    int bytesRead = stream.Read(buffer, 0, bytesToRead);

                                    if (bytesRead == 0)
                                    {
                                        // If no bytes are read before receiving the expected file size, it's a cancellation
                                        if (totalBytesReceived < fileSize)
                                        {
                                            fs.Close();
                                            File.Delete(savePath);
                                            MessageBox.Show($"Transfer of file '{fileName}' was canceled by the sender.");
                                            ResetProgressBar(progressBar, lblSpeed);
                                            break;
                                        }
                                        else
                                        {
                                            // Finished reading the file
                                            break;
                                        }
                                    }

                                    fs.Write(buffer, 0, bytesRead);
                                    totalBytesReceived += bytesRead;

                                    // Update progress bar and speed
                                    long bytesReceivedCopy = totalBytesReceived;
                                    progressBar.Invoke((MethodInvoker)(() =>
                                    {
                                        progressBar.Value = (int)Math.Min(bytesReceivedCopy, progressBar.Maximum);
                                    }));
                                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                    if (elapsedSeconds > 0)
                                    {
                                        double speed = (bytesReceivedCopy / 1024.0 / 1024.0) / elapsedSeconds;
                                        lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                    }
                                }

                                // Check if the transfer completed successfully
                                if (totalBytesReceived == fileSize)
                                {
                                    receivedFiles.Add(savePath);
                                }
                                else
                                {
                                    // If the file was not fully received, it was canceled
                                    fs.Close();
                                    File.Delete(savePath);
                                    // Cancellation message has already been displayed
                                    break; // Exit the loop if a transfer was canceled
                                }

                                ResetProgressBar(progressBar, lblSpeed);
                            }
                        }

                        // Display a summary message of the received files
                        if (receivedFiles.Count > 0)
                        {
                            string receivedFilesList = string.Join("\n", receivedFiles);
                            MessageBox.Show($"Files received:\n{receivedFilesList}");
                        }
                        else
                        {
                            MessageBox.Show("No files were received.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error receiving files: {ex.Message}");
                if (isServerRunning)
                {
                    MessageBox.Show($"Error receiving files: {ex.Message}");
                }
            }
            finally
            {
                lock (connectedClients)
                {
                    connectedClients.Remove(client);
                }
                client.Close();
            }
        }

        // Method to send multiple files sequentially with metadata sent at the beginning
        public static void SendFiles(string ipAddress, List<string> filePaths, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled)
        {
            Thread sendThread = new Thread(() =>
            {
                try
                {
                    Debug.WriteLine($"Attempting to connect to {ipAddress} to send files.");
                    using (TcpClient client = new TcpClient())
                    {
                        client.NoDelay = true;  // Disable Nagle's algorithm for better performance
                        client.ReceiveBufferSize = buffer.Length;
                        client.SendBufferSize = buffer.Length;
                        client.Connect(ipAddress, 5001);

                        using (NetworkStream stream = client.GetStream())
                        {
                            // Send the number of files
                            int fileCount = filePaths.Count;
                            byte[] fileCountBytes = BitConverter.GetBytes(fileCount);
                            stream.Write(fileCountBytes, 0, fileCountBytes.Length);

                            // Send filenames and their sizes
                            foreach (string filePath in filePaths)
                            {
                                if (!File.Exists(filePath))
                                {
                                    MessageBox.Show($"The file {filePath} does not exist.");
                                    continue;
                                }

                                string fileName = Path.GetFileName(filePath);
                                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                                int fileNameLength = fileNameBytes.Length;
                                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);
                                long fileSize = new FileInfo(filePath).Length;
                                byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);

                                // Send the filename length, name, and file size
                                stream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                                stream.Write(fileNameBytes, 0, fileNameBytes.Length);
                                stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                            }

                            List<string> sentFiles = new List<string>();

                            foreach (string filePath in filePaths)
                            {
                                if (isCancelled())
                                {
                                    MessageBox.Show("Sending canceled.");
                                    break;
                                }

                                if (!File.Exists(filePath))
                                {
                                    continue; // The file doesn't exist, skip to the next one
                                }

                                string fileName = Path.GetFileName(filePath);
                                long fileSize = new FileInfo(filePath).Length;

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
                                        progressBar.Maximum = (int)Math.Max(fileSize, 1); // Ensure maximum is at least 1
                                    }));

                                    // Send the file
                                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        if (isCancelled())
                                        {
                                            // Notify cancellation and close the connection
                                            MessageBox.Show("Sending canceled.");
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
                                                MessageBox.Show("The transfer was canceled by the receiver.");
                                            }
                                            else
                                            {
                                                MessageBox.Show("Sending canceled.");
                                            }
                                            ResetProgressBar(progressBar, lblSpeed);
                                            return;
                                        }

                                        totalBytesSent += bytesRead;

                                        // Update progress bar and speed
                                        long bytesSentCopy = totalBytesSent;
                                        progressBar.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBar.Value = (int)Math.Min(bytesSentCopy, progressBar.Maximum);
                                        }));
                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                        if (elapsedSeconds > 0)
                                        {
                                            double speed = (bytesSentCopy / 1024.0 / 1024.0) / elapsedSeconds; // Speed in MB/s
                                            lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                        }
                                    }

                                    sentFiles.Add(filePath);
                                    ResetProgressBar(progressBar, lblSpeed);
                                }
                            }

                            // Display a summary message of the sent files
                            if (sentFiles.Count > 0)
                            {
                                string sentFilesList = string.Join("\n", sentFiles);
                                MessageBox.Show($"Files sent successfully:\n{sentFilesList}");
                            }
                            else
                            {
                                MessageBox.Show("No files were sent.");
                            }
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
                        MessageBox.Show($"Error sending files: {ex.Message}");
                    }
                    ResetProgressBar(progressBar, lblSpeed);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unknown error: {ex.Message}");
                    if (!isCancelled())
                    {
                        MessageBox.Show($"Error sending files: {ex.Message}");
                    }
                    ResetProgressBar(progressBar, lblSpeed);
                }
            });
            sendThread.IsBackground = true;
            sendThread.Start();
        }

        // Resets the progress bar and speed display
        private static void ResetProgressBar(ProgressBar progressBar, Label lblSpeed)
        {
            progressBar.Invoke((MethodInvoker)(() =>
            {
                progressBar.Value = 0;
                progressBar.Maximum = 100; // Default value to avoid issues
                lblSpeed.Text = "Speed: 0 MB/s";
            }));
        }

        // Generates a unique file path if the file already exists
        private static string GetUniqueFilePath(string directory, string fileName)
        {
            string filePath = Path.Combine(directory, fileName);
            string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int count = 1;

            while (File.Exists(filePath))
            {
                string tempFileName = $"{fileNameOnly}({count}){extension}";
                filePath = Path.Combine(directory, tempFileName);
                count++;
            }

            return filePath;
        }
    }
} //for comm
