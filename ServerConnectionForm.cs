using System;
using System.Windows.Forms;

namespace UTransfer
{
    public partial class ServerConnectionForm : Form
    {
        public ServerConnectionForm()
        {
            InitializeComponent();
        }

        // This method is called when the user clicks "Stop"
        private void buttonStop_Click(object sender, EventArgs e)
        {
            // Stop the receiving server
            NetworkHelper.StopServer();
            this.Close();  // Close the window after stopping the server
        }

        private void ServerConnectionForm_Load(object sender, EventArgs e)
        {

        }
    }
}
