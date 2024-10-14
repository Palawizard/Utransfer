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
            NetworkHelper.RunServerInThread();  // Démarre le serveur et affiche un message de confirmation
        }

        private void buttonArreter_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Arrêter' cliqué. Arrêt du serveur.");
            NetworkHelper.StopServer();  // Arrête le serveur et affiche un message de confirmation
        }
    }
}
