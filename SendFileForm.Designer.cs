namespace UTransfer
{
    partial class SendFileForm
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
            txtIpAddress = new TextBox();
            btnEnvoyer = new Button();
            btnAnnuler = new Button();
            labelIp = new Label();
            progressBar = new ProgressBar();
            lblSpeed = new Label();
            SuspendLayout();
            // 
            // txtIpAddress
            // 
            txtIpAddress.Location = new Point(34, 47);
            txtIpAddress.Name = "txtIpAddress";
            txtIpAddress.Size = new Size(176, 23);
            txtIpAddress.TabIndex = 0;
            // 
            // btnEnvoyer
            // 
            btnEnvoyer.Location = new Point(58, 138);
            btnEnvoyer.Name = "btnEnvoyer";
            btnEnvoyer.Size = new Size(131, 28);
            btnEnvoyer.TabIndex = 1;
            btnEnvoyer.Text = "Envoyer";
            btnEnvoyer.UseVisualStyleBackColor = true;
            btnEnvoyer.Click += btnEnvoyer_Click;
            // 
            // btnAnnuler
            // 
            btnAnnuler.Location = new Point(58, 172);
            btnAnnuler.Name = "btnAnnuler";
            btnAnnuler.Size = new Size(131, 28);
            btnAnnuler.TabIndex = 5;
            btnAnnuler.Text = "Annuler";
            btnAnnuler.UseVisualStyleBackColor = true;
            btnAnnuler.Click += btnAnnuler_Click;
            // 
            // labelIp
            // 
            labelIp.AutoSize = true;
            labelIp.Location = new Point(34, 29);
            labelIp.Name = "labelIp";
            labelIp.Size = new Size(94, 15);
            labelIp.TabIndex = 2;
            labelIp.Text = "Adresse IP Amis:";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(34, 85);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(175, 23);
            progressBar.TabIndex = 3;
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(34, 111);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(84, 15);
            lblSpeed.TabIndex = 4;
            lblSpeed.Text = "Vitesse : 0 kB/s";
            // 
            // SendFileForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(248, 234);
            Controls.Add(lblSpeed);
            Controls.Add(progressBar);
            Controls.Add(labelIp);
            Controls.Add(txtIpAddress);
            Controls.Add(btnAnnuler);
            Controls.Add(btnEnvoyer);
            Name = "SendFileForm";
            Text = "Envoyer un fichier";
            Load += SendFileForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnEnvoyer;
        private System.Windows.Forms.Button btnAnnuler;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblSpeed;
    }
}
