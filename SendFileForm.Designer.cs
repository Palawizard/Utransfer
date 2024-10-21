namespace UTransfer
{
    partial class SendFileForm
    {
        private System.ComponentModel.IContainer components = null;

        // Existing controls
        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblSpeed;

        // New controls
        private System.Windows.Forms.CheckBox chkSaveForLater;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.ComboBox cmbSavedContacts;
        private System.Windows.Forms.Label lblSavedContacts;

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
            this.btnSend = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelIp = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.chkSaveForLater = new System.Windows.Forms.CheckBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.cmbSavedContacts = new System.Windows.Forms.ComboBox();
            this.lblSavedContacts = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Location = new System.Drawing.Point(34, 47);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(176, 23);
            this.txtIpAddress.TabIndex = 0;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(58, 270);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(131, 28);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(58, 304);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(131, 28);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelIp
            // 
            this.labelIp.AutoSize = true;
            this.labelIp.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelIp.Location = new System.Drawing.Point(34, 29);
            this.labelIp.Name = "labelIp";
            this.labelIp.Size = new System.Drawing.Size(94, 15);
            this.labelIp.TabIndex = 2;
            this.labelIp.Text = "Recipient's IP:";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(34, 203);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(175, 23);
            this.progressBar.TabIndex = 3;
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(34, 229);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(77, 15);
            this.lblSpeed.TabIndex = 4;
            this.lblSpeed.Text = "Speed: 0 kB/s";
            // 
            // chkSaveForLater
            // 
            this.chkSaveForLater.AutoSize = true;
            this.chkSaveForLater.Location = new System.Drawing.Point(34, 76);
            this.chkSaveForLater.Name = "chkSaveForLater";
            this.chkSaveForLater.Size = new System.Drawing.Size(99, 19);
            this.chkSaveForLater.TabIndex = 6;
            this.chkSaveForLater.Text = "Save for later";
            this.chkSaveForLater.UseVisualStyleBackColor = true;
            this.chkSaveForLater.CheckedChanged += new System.EventHandler(this.chkSaveForLater_CheckedChanged);
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(34, 127);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(176, 23);
            this.txtUsername.TabIndex = 7;
            this.txtUsername.Visible = false;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(34, 109);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(66, 15);
            this.lblUsername.TabIndex = 8;
            this.lblUsername.Text = "Username:";
            this.lblUsername.Visible = false;
            // 
            // cmbSavedContacts
            // 
            this.cmbSavedContacts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSavedContacts.FormattingEnabled = true;
            this.cmbSavedContacts.Location = new System.Drawing.Point(34, 177);
            this.cmbSavedContacts.Name = "cmbSavedContacts";
            this.cmbSavedContacts.Size = new System.Drawing.Size(176, 23);
            this.cmbSavedContacts.TabIndex = 9;
            this.cmbSavedContacts.SelectedIndexChanged += new System.EventHandler(this.cmbSavedContacts_SelectedIndexChanged);
            // 
            // lblSavedContacts
            // 
            this.lblSavedContacts.AutoSize = true;
            this.lblSavedContacts.Location = new System.Drawing.Point(34, 159);
            this.lblSavedContacts.Name = "lblSavedContacts";
            this.lblSavedContacts.Size = new System.Drawing.Size(89, 15);
            this.lblSavedContacts.TabIndex = 10;
            this.lblSavedContacts.Text = "Saved contacts:";
            // 
            // SendFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(248, 350);
            this.Controls.Add(this.lblSavedContacts);
            this.Controls.Add(this.cmbSavedContacts);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.chkSaveForLater);
            this.Controls.Add(this.lblSpeed);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.labelIp);
            this.Controls.Add(this.txtIpAddress);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSend);
            this.Name = "SendFileForm";
            this.Text = "Send a File";
            this.Load += new System.EventHandler(this.SendFileForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
