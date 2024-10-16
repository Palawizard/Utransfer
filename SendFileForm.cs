using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        private bool isCancelled = false;

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
            List<string> filePaths = SelectFiles();  // Open a dialog to select files

            if (!string.IsNullOrEmpty(ipAddress) && filePaths.Count > 0)
            {
                Debug.WriteLine($"Sending files to {ipAddress}");

                // Reset the progress bar before sending
                progressBar.Value = 0;
                lblSpeed.Text = "Speed: 0 kB/s";
                isCancelled = false;
                // Use the SendFiles method to send multiple files sequentially
                NetworkHelper.SendFiles(ipAddress, filePaths, progressBar, lblSpeed, () => isCancelled);
            }
            else
            {
                MessageBox.Show("Please enter a valid IP address and select at least one file.");
            }
        }

        // Method to open a dialog to select multiple files
        private List<string> SelectFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true  // Allow multiple file selection
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return new List<string>(openFileDialog.FileNames);  // Return the selected files' paths
            }
            return new List<string>();
        }

        // Method to cancel the sending process
        private void btnCancel_Click(object sender, EventArgs e)
        {
            isCancelled = true;  // Mark the transfer as canceled
            MessageBox.Show("Sending canceled.");
        }
    }
} //for commit
