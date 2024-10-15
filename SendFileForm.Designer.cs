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
            this.txtIpAddress = new System.Windows.Forms.TextBox();
            this.btnEnvoyer = new System.Windows.Forms.Button();
            this.labelIp = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Location = new System.Drawing.Point(20, 50);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(200, 22);
            this.txtIpAddress.TabIndex = 0;
            // 
            // btnEnvoyer
            // 
            this.btnEnvoyer.Location = new System.Drawing.Point(20, 130);
            this.btnEnvoyer.Name = "btnEnvoyer";
            this.btnEnvoyer.Size = new System.Drawing.Size(150, 30);
            this.btnEnvoyer.TabIndex = 1;
            this.btnEnvoyer.Text = "Envoyer";
            this.btnEnvoyer.UseVisualStyleBackColor = true;
            this.btnEnvoyer.Click += new System.EventHandler(this.btnEnvoyer_Click);
            // 
            // labelIp
            // 
            this.labelIp.AutoSize = true;
            this.labelIp.Location = new System.Drawing.Point(20, 30);
            this.labelIp.Name = "labelIp";
            this.labelIp.Size = new System.Drawing.Size(108, 17);
            this.labelIp.TabIndex = 2;
            this.labelIp.Text = "Adresse IP Amis:";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 90);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(200, 25);
            this.progressBar.TabIndex = 3;
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(20, 170);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(82, 17);
            this.lblSpeed.TabIndex = 4;
            this.lblSpeed.Text = "Vitesse : 0 kB/s";
            // 
            // SendFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 200);
            this.Controls.Add(this.lblSpeed);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.labelIp);
            this.Controls.Add(this.txtIpAddress);
            this.Controls.Add(this.btnEnvoyer);
            this.Name = "SendFileForm";
            this.Text = "Envoyer un fichier";
            this.Load += new System.EventHandler(this.SendFileForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnEnvoyer;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblSpeed;
    }
}
