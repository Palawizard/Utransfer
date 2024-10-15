using System;
using System.Windows.Forms;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UTransfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Fenêtre principale chargée.");
        }

        private void buttonEnvoyer_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Envoyer' cliqué.");
            SendFileForm sendForm = new SendFileForm();
            sendForm.ShowDialog();  // Ouvre la fenêtre d'envoi
        }

        private void buttonRecevoir_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Recevoir' cliqué. Démarrage du serveur de réception.");

            // Appel modifié pour inclure la ProgressBar et le Label de vitesse
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonArreter_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Arrêter' cliqué. Arrêt du serveur.");
            NetworkHelper.StopServer();  // Arrête le serveur et affiche un message de confirmation
        }
    }
}
