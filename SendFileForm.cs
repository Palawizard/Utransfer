using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        public SendFileForm()
        {
            InitializeComponent();
        }

        // Méthode qui s'exécute lors du chargement de la fenêtre
        private void SendFileForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Fenêtre SendFileForm chargée.");
        }

        // Méthode qui se déclenche lorsque l'utilisateur clique sur le bouton "Envoyer"
        private void btnEnvoyer_Click(object sender, EventArgs e)
        {
            string ipAddress = txtIpAddress.Text;  // Récupère l'adresse IP saisie
            string filePath = SelectFile();  // Ouvre une boîte de dialogue pour sélectionner un fichier

            if (!string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine($"Envoi du fichier {filePath} à {ipAddress}");
                NetworkHelper.SendFile(ipAddress, filePath);  // Appelle la méthode d'envoi dans NetworkHelper
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
    }
}
