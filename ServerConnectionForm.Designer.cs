namespace UTransfer
{
    partial class ServerConnectionForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            labelMessage = new Label();
            buttonArreter = new Button();
            SuspendLayout();
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.Location = new Point(49, 31);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(186, 15);
            labelMessage.TabIndex = 0;
            labelMessage.Text = "Serveur en attente de connexion...";
            // 
            // buttonArreter
            // 
            buttonArreter.Location = new Point(90, 70);
            buttonArreter.Name = "buttonArreter";
            buttonArreter.Size = new Size(100, 30);
            buttonArreter.TabIndex = 1;
            buttonArreter.Text = "Arrêter";
            buttonArreter.UseVisualStyleBackColor = true;
            buttonArreter.Click += buttonArreter_Click;
            // 
            // ServerConnectionForm
            // 
            ClientSize = new Size(284, 131);
            Controls.Add(buttonArreter);
            Controls.Add(labelMessage);
            Name = "ServerConnectionForm";
            Text = "Connexion au serveur";
            Load += ServerConnectionForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Button buttonArreter;
    }
}
