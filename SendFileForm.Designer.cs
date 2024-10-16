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
            btnSend = new Button();
            btnCancel = new Button();
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
            // btnSend
            // 
            btnSend.Location = new Point(58, 138);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(131, 28);
            btnSend.TabIndex = 1;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(58, 172);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(131, 28);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // labelIp
            // 
            labelIp.AutoSize = true;
            labelIp.Location = new Point(34, 29);
            labelIp.Name = "labelIp";
            labelIp.Size = new Size(94, 15);
            labelIp.TabIndex = 2;
            labelIp.Text = "Friend's IP Address:";
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
            lblSpeed.Text = "Speed: 0 kB/s";
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
            Controls.Add(btnCancel);
            Controls.Add(btnSend);
            Name = "SendFileForm";
            Text = "Send a File";
            Load += SendFileForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblSpeed;
    }
}
