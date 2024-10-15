using System;
using System.Windows.Forms;
using System.Diagnostics;

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
            progressBar1.Visible = false;  // Cache la barre de progression au départ
            lblSpeed.Visible = false;  // Cache également le label de vitesse au départ
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

            // Affiche la barre de progression et le label de vitesse uniquement lorsque le serveur est démarré
            progressBar1.Visible = true;
            lblSpeed.Visible = true;
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonArreter_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Arrêter' cliqué. Arrêt du serveur.");
            NetworkHelper.StopServer();  // Arrête le serveur et affiche un message de confirmation

            // Cache la barre de progression et le label de vitesse après l'arrêt du serveur
            progressBar1.Visible = false;
            lblSpeed.Visible = false;
        }
    }
}
