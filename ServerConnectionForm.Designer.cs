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
            this.labelMessage = new System.Windows.Forms.Label();
            this.buttonArreter = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelMessage
            // 
            this.labelMessage.AutoSize = true;
            this.labelMessage.Location = new System.Drawing.Point(40, 30);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(210, 17);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "Serveur en attente de connexion...";
            // 
            // buttonArreter
            // 
            this.buttonArreter.Location = new System.Drawing.Point(90, 70);
            this.buttonArreter.Name = "buttonArreter";
            this.buttonArreter.Size = new System.Drawing.Size(100, 30);
            this.buttonArreter.TabIndex = 1;
            this.buttonArreter.Text = "Arrêter";
            this.buttonArreter.UseVisualStyleBackColor = true;
            this.buttonArreter.Click += new System.EventHandler(this.buttonArreter_Click);
            // 
            // ServerConnectionForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 131);
            this.Controls.Add(this.buttonArreter);
            this.Controls.Add(this.labelMessage);
            this.Name = "ServerConnectionForm";
            this.Text = "Connexion au serveur";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Button buttonArreter;
    }
}
