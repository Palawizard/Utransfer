using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Main window loaded.");
            // progressBar1 and lblSpeed will remain visible even when the server is not started
            progressBar1.Value = 0;  // Reset the progress bar to 0 initially
            lblSpeed.Text = "Speed: 0 kB/s";  // Set the initial speed label
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Send button clicked.");
            SendFileForm sendForm = new SendFileForm();
            sendForm.ShowDialog();  // Open the send file window
        }

        private void buttonReceive_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Receive button clicked. Starting the receiving server.");

            // Start the server without affecting the visibility of the progress bar or speed label
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Stop button clicked. Stopping the server.");
            NetworkHelper.StopServer();  // Stop the server and show a confirmation message

            // Keep the progress bar and speed label visible even after stopping the server
            progressBar1.Value = 0;  // Reset the progress bar after stopping the server
            lblSpeed.Text = "Speed: 0 kB/s";  // Reset the speed label after stopping the server
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
} //for commit
