using System;
using System.Windows.Forms;

namespace UTransfer
{
    public partial class ServerConnectionForm : Form
    {
        public ServerConnectionForm()
        {
            InitializeComponent();
        }

        // Cette méthode sera appelée quand l'utilisateur clique sur "Arrêter"
        private void buttonArreter_Click(object sender, EventArgs e)
        {
            // Arrêter le serveur de réception
            NetworkHelper.StopServer();
            this.Close();  // Ferme la fenêtre après l'arrêt du serveur
        }
    }
}
