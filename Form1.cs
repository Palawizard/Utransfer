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
            Debug.WriteLine("Fen�tre principale charg�e.");
            progressBar1.Visible = false;  // Cache la barre de progression au d�part
            lblSpeed.Visible = false;  // Cache �galement le label de vitesse au d�part
        }

        private void buttonEnvoyer_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Envoyer' cliqu�.");
            SendFileForm sendForm = new SendFileForm();
            sendForm.ShowDialog();  // Ouvre la fen�tre d'envoi
        }

        private void buttonRecevoir_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Recevoir' cliqu�. D�marrage du serveur de r�ception.");

            // Affiche la barre de progression et le label de vitesse uniquement lorsque le serveur est d�marr�
            progressBar1.Visible = true;
            lblSpeed.Visible = true;
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonArreter_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Arr�ter' cliqu�. Arr�t du serveur.");
            NetworkHelper.StopServer();  // Arr�te le serveur et affiche un message de confirmation

            // Cache la barre de progression et le label de vitesse apr�s l'arr�t du serveur
            progressBar1.Visible = false;
            lblSpeed.Visible = false;
        }
    }
}
