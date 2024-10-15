using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        private bool isCancelled = false;
        private Task? sendFileTask;  // Remplacer le thread par une tâche asynchrone

        public SendFileForm()
        {
            InitializeComponent();
        }

        private void SendFileForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Fenêtre SendFileForm chargée.");
        }

        private async void btnEnvoyer_Click(object sender, EventArgs e)
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

                // Démarrer l'envoi du fichier en mode asynchrone
                sendFileTask = NetworkHelper.SendFileAsync(ipAddress, filePath, progressBar, lblSpeed, () => isCancelled);
                await sendFileTask;
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
            isCancelled = true;  // Marquer le transfert comme annulé
            MessageBox.Show("Envoi annulé.");
        }
    }
}
