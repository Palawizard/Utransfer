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

            try
            {
                // Ask the user to enter a passphrase
                string? passphrase = PromptForPassphrase();
                if (passphrase == null)
                {
                    // The user canceled the input
                    MessageBox.Show("Operation canceled. A passphrase is required to start the server.");
                    return;
                }

                // Start the server with the passphrase
                NetworkHelper.RunServerInThread(progressBar1, lblSpeed, passphrase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting the server: {ex.Message}");
                MessageBox.Show($"Error starting the server: {ex.Message}");
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Stop button clicked. Stopping the server.");
            try
            {
                NetworkHelper.StopServer();  // Stop the server and show a confirmation message

                // Keep the progress bar and speed label visible even after stopping the server
                progressBar1.Value = 0;  // Reset the progress bar after stopping the server
                lblSpeed.Text = "Speed: 0 kB/s";  // Reset the speed label after stopping the server
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping the server: {ex.Message}");
                MessageBox.Show($"Error stopping the server: {ex.Message}");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // Method to prompt the user for a passphrase
        private string? PromptForPassphrase()
        {
            using (PassphraseForm passphraseForm = new PassphraseForm())
            {
                if (passphraseForm.ShowDialog() == DialogResult.OK)
                {
                    return passphraseForm.Passphrase;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
