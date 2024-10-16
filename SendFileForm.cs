using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        private bool isCancelled = false;
        private Thread? sendFileThread;  // Declared as nullable

        public SendFileForm()
        {
            InitializeComponent();
        }

        private void SendFileForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("SendFileForm window loaded.");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string ipAddress = txtIpAddress.Text;  // Get the entered IP address
            string filePath = SelectFile();  // Open a dialog to select a file

            if (!string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine($"Sending file {filePath} to {ipAddress}");

                // Reset the progress bar before sending
                progressBar.Value = 0;
                lblSpeed.Text = "Speed: 0 kB/s";
                isCancelled = false;

                // Create a separate thread for sending the file
                sendFileThread = new Thread(() =>
                {
                    // Use the SendFile method (not SendFileAsync)
                    NetworkHelper.SendFile(ipAddress, filePath, progressBar, lblSpeed, () => isCancelled);
                });
                sendFileThread.IsBackground = true;
                sendFileThread.Start();
            }
            else
            {
                MessageBox.Show("Please enter a valid IP address and select a file.");
            }
        }

        // Method to open a dialog to select a file
        private string SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;  // Return the selected file's path
            }
            return string.Empty;
        }

        // Method to cancel the sending process
        private void btnCancel_Click(object sender, EventArgs e)
        {
            isCancelled = true;  // Mark the transfer as canceled
            MessageBox.Show("Sending canceled.");
        }
    }
}
