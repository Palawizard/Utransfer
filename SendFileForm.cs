using System;
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
            Debug.WriteLine("Fenêtre SendFileForm chargée.");
        }

        private void btnEnvoyer_Click(object sender, EventArgs e)
        {
            string ipAddress = txtIpAddress.Text;  // Récupère l'adresse IP saisie
            string filePath = SelectFile();  // Ouvre une boîte de dialogue pour sélectionner un fichier

            if (!string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine($"Envoi du fichier {filePath} à {ipAddress}");

                // Réinitialiser la barre de progression avant l'envoi
                progressBar.Value = 0;
                lblSpeed.Text = "Vitesse : 0 kB/s";
                isCancelled = false;

                // Appelle la méthode d'envoi dans NetworkHelper en passant la ProgressBar et le Label
                NetworkHelper.SendFile(ipAddress, filePath, progressBar, lblSpeed, () => isCancelled);
            }
            else
            {
                MessageBox.Show("Veuillez entrer une adresse IP valide et sélectionner un fichier.");
            }
        }

        // Méthode pour ouvrir une boîte de dialogue pour sélectionner un fichier
        private string SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;  // Renvoie le chemin du fichier sélectionné
            }
            return string.Empty;
        }

        // Méthode pour annuler l'envoi
        private void btnAnnuler_Click(object sender, EventArgs e)
        {
            isCancelled = true;  // Annule l'envoi
            MessageBox.Show("Envoi annulé.");
        }
    }
}
