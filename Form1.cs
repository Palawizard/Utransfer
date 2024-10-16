using System;
using System.Windows.Forms;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            progressBar1.Visible = false;  // Hide the progress bar initially
            lblSpeed.Visible = false;  // Hide the speed label initially
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

            // Show the progress bar and speed label only when the server is started
            progressBar1.Visible = true;
            lblSpeed.Visible = true;
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Stop button clicked. Stopping the server.");
            NetworkHelper.StopServer();  // Stop the server and show a confirmation message

            // Hide the progress bar and speed label after stopping the server
            progressBar1.Visible = false;
            lblSpeed.Visible = false;
        }
    }
}
