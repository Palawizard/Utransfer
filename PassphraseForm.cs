using System;
using System.Windows.Forms;

namespace UTransfer
{
    public partial class PassphraseForm : Form
    {
        public string? Passphrase { get; private set; } // Declared as nullable

        public PassphraseForm()
        {
            InitializeComponent();
            Passphrase = null; // Explicit initialization
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Passphrase = txtPassphrase.Text;
            if (string.IsNullOrEmpty(Passphrase))
            {
                MessageBox.Show("Please enter a passphrase.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Passphrase = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void lblPrompt_Click(object sender, EventArgs e)
        {

        }
    }
}
