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
            Debug.WriteLine("Fen�tre principale charg�e.");
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

            // Appel modifi� pour inclure la ProgressBar et le Label de vitesse
            NetworkHelper.RunServerInThread(progressBar1, lblSpeed);
        }

        private void buttonArreter_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Bouton 'Arr�ter' cliqu�. Arr�t du serveur.");
            NetworkHelper.StopServer();  // Arr�te le serveur et affiche un message de confirmation
        }
    }
}
