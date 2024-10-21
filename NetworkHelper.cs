using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace UTransfer
{
    public static class NetworkHelper
    {
        private static TcpListener? listener;
        private static bool isServerRunning = false;
        private static Thread? serverThread;
        private static readonly byte[] buffer = new byte[1048576]; // Optimized buffer of 1 MB
        private static List<TcpClient> connectedClients = new List<TcpClient>(); // Tracking connected clients
        private static string? serverPassphrase = null;

        // Starts the receiving server with an optimized buffer and a passphrase
        public static void RunServerInThread(ProgressBar progressBar, Label lblSpeed, string? passphrase)
        {
            if (!isServerRunning)
            {
                isServerRunning = true;
                serverPassphrase = passphrase; // Store the passphrase
                serverThread = new Thread(() => StartServer(progressBar, lblSpeed));
                serverThread.IsBackground = true;
                serverThread.Priority = ThreadPriority.Highest; // Maximize performance
                serverThread.Start();
                MessageBox.Show("Receiving server started.");
            }
            else
            {
                MessageBox.Show("The server is already running.");
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
                            MessageBox.Show($"Socket error while accepting a connection: {ex.Message}");
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
                Debug.WriteLine($"Error starting the server: {ex.Message}");
                MessageBox.Show($"Error starting the server: {ex.Message}");
            }
        }

        // Helper method to read exactly 'size' bytes from the stream
        private static void ReadExact(Stream stream, byte[] buffer, int offset, int size)
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
                using (NetworkStream networkStream = client.GetStream())
                {
                    // Read the IV from the stream
                    byte[] iv = new byte[16]; // AES block size is 16 bytes
                    ReadExact(networkStream, iv, 0, iv.Length);

                    // Derive the key from the passphrase
                    if (serverPassphrase == null)
                    {
                        // If passphrase is null, close the connection
                        client.Close();
                        return;
                    }

                    byte[] key = DeriveKeyFromPassphrase(serverPassphrase, iv);

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;

                        using (CryptoStream cryptoStream = new CryptoStream(networkStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            try
                            {
                                // Wrap reading operations in a try-catch to handle incorrect passphrase
                                // Read the number of files
                                byte[] fileCountBytes = new byte[4];
                                ReadExact(cryptoStream, fileCountBytes, 0, 4);
                                int fileCount = BitConverter.ToInt32(fileCountBytes, 0);

                                // Read file names and their sizes
                                List<string> fileNames = new List<string>();
                                List<long> fileSizes = new List<long>();
                                for (int i = 0; i < fileCount; i++)
                                {
                                    // Read the length of the file name
                                    byte[] fileNameLengthBytes = new byte[4];
                                    ReadExact(cryptoStream, fileNameLengthBytes, 0, 4);
                                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                                    // Read the file name
                                    byte[] fileNameBytes = new byte[fileNameLength];
                                    ReadExact(cryptoStream, fileNameBytes, 0, fileNameLength);
                                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                                    // Read the file size
                                    byte[] fileSizeBytes = new byte[8];
                                    ReadExact(cryptoStream, fileSizeBytes, 0, 8);
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

                                        // Get a unique file path to avoid overwriting existing files
                                        string savePath = GetUniqueFilePath(saveFolderPath, fileName);

                                        try
                                        {
                                            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                                            {
                                                long totalBytesReceived = 0;
                                                Stopwatch stopwatch = new Stopwatch();
                                                stopwatch.Start();

                                                progressBar.Invoke((MethodInvoker)(() =>
                                                {
                                                    progressBar.Value = 0;
                                                    progressBar.Maximum = 1000000; // Fixed maximum for scaling
                                                }));

                                                try
                                                {
                                                    while (totalBytesReceived < fileSize)
                                                    {
                                                        if (!isServerRunning)
                                                        {
                                                            // Server stopped, cancel the transfer
                                                            fs.Close();
                                                            File.Delete(savePath);
                                                            // Notify sender that transfer was canceled
                                                            break;
                                                        }

                                                        int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesReceived);
                                                        int bytesRead = cryptoStream.Read(buffer, 0, bytesToRead);

                                                        if (bytesRead == 0)
                                                        {
                                                            // If no bytes are read before receiving the expected file size, it's a cancellation
                                                            if (totalBytesReceived < fileSize)
                                                            {
                                                                fs.Close();
                                                                File.Delete(savePath);
                                                                // Transfer was interrupted
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                // File reading completed
                                                                break;
                                                            }
                                                        }

                                                        fs.Write(buffer, 0, bytesRead);
                                                        totalBytesReceived += bytesRead;

                                                        // Update the progress bar and speed
                                                        double progressPercentage = (double)totalBytesReceived / fileSize * 1000000;
                                                        progressBar.Invoke((MethodInvoker)(() =>
                                                        {
                                                            progressBar.Value = (int)Math.Min(progressPercentage, progressBar.Maximum);
                                                        }));

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
                                                        receivedFiles.Add(savePath);
                                                    }
                                                    else
                                                    {
                                                        // If the file was not fully received, it was canceled
                                                        fs.Close();
                                                        File.Delete(savePath);
                                                        // Transfer was canceled
                                                        break; // Exit the loop if a transfer was canceled
                                                    }
                                                }
                                                finally
                                                {
                                                    // Reset progress bar regardless of how the transfer ended
                                                    ResetProgressBar(progressBar, lblSpeed);
                                                }
                                            }
                                        }
                                        catch (IOException ex)
                                        {
                                            Debug.WriteLine($"I/O error while receiving the file '{fileName}': {ex.Message}");
                                            MessageBox.Show($"Error receiving the file '{fileName}': {ex.Message}");
                                            continue;
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Error receiving the file '{fileName}': {ex.Message}");
                                            MessageBox.Show($"Error receiving the file '{fileName}': {ex.Message}");
                                            continue;
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
                                else
                                {
                                    // User declined to receive files; close the connection
                                    client.Close();
                                }
                            }
                            catch (Exception ex) when (ex is CryptographicException || ex is IOException || ex is OverflowException || ex is ArgumentOutOfRangeException)
                            {
                                // Passphrase is incorrect or data is corrupted; close the connection silently
                                client.Close();
                                return;
                            }
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"Socket error while receiving files: {ex.Message}");
                MessageBox.Show($"Socket error: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"I/O error while receiving files: {ex.Message}");
                MessageBox.Show($"I/O error while receiving files: {ex.Message}");
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
        public static void SendFiles(string ipAddress, List<string> filePaths, ProgressBar progressBar, Label lblSpeed, Func<bool> isCancelled, string passphrase)
        {
            Thread sendThread = new Thread(() =>
            {
                bool isMetadataSent = false; // Indicates whether metadata has been sent

                try
                {
                    Debug.WriteLine($"Attempting to connect to {ipAddress} to send files.");
                    using (TcpClient client = new TcpClient())
                    {
                        client.NoDelay = true;  // Disable Nagle's algorithm for better performance
                        client.ReceiveBufferSize = buffer.Length;
                        client.SendBufferSize = buffer.Length;
                        client.Connect(ipAddress, 5001);

                        using (NetworkStream networkStream = client.GetStream())
                        {
                            // Generate a random IV
                            byte[] iv = new byte[16]; // AES block size is 16 bytes
                            RandomNumberGenerator.Fill(iv);

                            // Send the IV to the receiver
                            networkStream.Write(iv, 0, iv.Length);

                            // Derive the key from the passphrase
                            byte[] key = DeriveKeyFromPassphrase(passphrase, iv);

                            using (Aes aes = Aes.Create())
                            {
                                aes.Key = key;
                                aes.IV = iv;

                                using (CryptoStream cryptoStream = new CryptoStream(networkStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                                {
                                    try
                                    {
                                        // Send the number of files
                                        int fileCount = filePaths.Count;
                                        byte[] fileCountBytes = BitConverter.GetBytes(fileCount);
                                        cryptoStream.Write(fileCountBytes, 0, fileCountBytes.Length);

                                        // Send file names and their sizes
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

                                            // Send the file name length, name, and size
                                            cryptoStream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                                            cryptoStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                                            cryptoStream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                                        }

                                        isMetadataSent = true; // Metadata has been successfully sent

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
                                                continue; // The file does not exist, skip to the next
                                            }

                                            string fileName = Path.GetFileName(filePath);
                                            long fileSize = new FileInfo(filePath).Length;

                                            try
                                            {
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
                                                        progressBar.Maximum = 1000000; // Fixed maximum for scaling
                                                    }));

                                                    // Send the file
                                                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                                    {
                                                        if (isCancelled())
                                                        {
                                                            // Notify cancellation and close the connection
                                                            MessageBox.Show("Sending canceled.");
                                                            cryptoStream.Close();  // Properly close the cryptoStream
                                                            client.Close();  // Close the client connection
                                                            ResetProgressBar(progressBar, lblSpeed);
                                                            return;
                                                        }

                                                        try
                                                        {
                                                            cryptoStream.Write(buffer, 0, bytesRead);
                                                            cryptoStream.Flush();
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
                                                                MessageBox.Show($"Error sending data: {ex.Message}");
                                                            }
                                                            ResetProgressBar(progressBar, lblSpeed);
                                                            return;
                                                        }

                                                        totalBytesSent += bytesRead;

                                                        // Update the progress bar and speed
                                                        double progressPercentage = (double)totalBytesSent / fileSize * 1000000;
                                                        progressBar.Invoke((MethodInvoker)(() =>
                                                        {
                                                            progressBar.Value = (int)Math.Min(progressPercentage, progressBar.Maximum);
                                                        }));

                                                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                                                        if (elapsedSeconds > 0)
                                                        {
                                                            double speed = (totalBytesSent / 1024.0 / 1024.0) / elapsedSeconds; // Speed in MB/s
                                                            lblSpeed.Invoke((MethodInvoker)(() => lblSpeed.Text = $"Speed: {speed:F2} MB/s"));
                                                        }
                                                    }

                                                    sentFiles.Add(filePath);
                                                    ResetProgressBar(progressBar, lblSpeed);
                                                }
                                            }
                                            catch (IOException ex)
                                            {
                                                Debug.WriteLine($"I/O error while sending the file '{fileName}': {ex.Message}");
                                                MessageBox.Show($"Error sending the file '{fileName}': {ex.Message}");
                                                ResetProgressBar(progressBar, lblSpeed);
                                                continue;
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Error sending the file '{fileName}': {ex.Message}");
                                                MessageBox.Show($"Error sending the file '{fileName}': {ex.Message}");
                                                ResetProgressBar(progressBar, lblSpeed);
                                                continue;
                                            }
                                        }

                                        // Ensure all data is flushed and encrypted properly
                                        cryptoStream.FlushFinalBlock();

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
                                    catch (IOException ex)
                                    {
                                        // Handle exceptions during metadata sending
                                        if (!isMetadataSent)
                                        {
                                            // Exception occurred while sending metadata
                                            MessageBox.Show("The passphrase is incorrect.");
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"I/O error during sending: {ex.Message}");
                                            MessageBox.Show($"I/O error while sending files: {ex.Message}");
                                        }
                                        ResetProgressBar(progressBar, lblSpeed);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Unknown error during sending: {ex.Message}");
                                        MessageBox.Show($"Unknown error while sending files: {ex.Message}");
                                        ResetProgressBar(progressBar, lblSpeed);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"Socket error: {ex.Message}");
                    MessageBox.Show($"Connection error to the server: {ex.Message}");
                }
                catch (IOException ex)
                {
                    // Handle exceptions during connection or initial data exchange
                    if (!isMetadataSent)
                    {
                        // Exception occurred before sending metadata
                        MessageBox.Show("The passphrase is incorrect.");
                    }
                    else
                    {
                        Debug.WriteLine($"I/O error: {ex.Message}");
                        if (!isCancelled())
                        {
                            MessageBox.Show($"Error sending files: {ex.Message}");
                        }
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

        // Derives a key from the passphrase using PBKDF2
        private static byte[] DeriveKeyFromPassphrase(string passphrase, byte[] salt)
        {
            // Use SHA256 as the hash algorithm and 100,000 iterations
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(passphrase, salt, 100000, HashAlgorithmName.SHA256))
            {
                return keyDerivationFunction.GetBytes(32); // 256-bit key for AES-256
            }
        }

        // Stops the server and properly closes all connections
        public static void StopServer()
        {
            if (listener != null && isServerRunning)
            {
                isServerRunning = false;
                serverPassphrase = null; // Clear the passphrase

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
            else
            {
                MessageBox.Show("The server is not running.");
            }
        }
    }
}
