using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        private bool isCancelled = false;
        private Thread? sendFileThread;  // Déclarer comme nullable

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

                // Crée un thread séparé pour l'envoi du fichier
                sendFileThread = new Thread(() =>
                {
                    NetworkHelper.SendFile(ipAddress, filePath, progressBar, lblSpeed, () => isCancelled);
                });
                sendFileThread.IsBackground = true;
                sendFileThread.Start();
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
            if (sendFileThread != null && sendFileThread.IsAlive)
            {
                sendFileThread.Join();  // Attend la fin du thread
            }
            MessageBox.Show("Envoi annulé.");
        }
    }
}
