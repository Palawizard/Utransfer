namespace UTransfer
{
    partial class MainForm
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
            buttonSend = new Button();
            buttonReceive = new Button();
            buttonStop = new Button();
            progressBar1 = new ProgressBar();
            lblSpeed = new Label();
            SuspendLayout();
            // 
            // buttonSend
            // 
            buttonSend.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonSend.Location = new Point(79, 59);
            buttonSend.Name = "buttonSend";
            buttonSend.Size = new Size(131, 25);
            buttonSend.TabIndex = 0;
            buttonSend.Text = "Send";
            buttonSend.UseVisualStyleBackColor = true;
            buttonSend.Click += buttonSend_Click;
            // 
            // buttonReceive
            // 
            buttonReceive.Location = new Point(79, 90);
            buttonReceive.Name = "buttonReceive";
            buttonReceive.Size = new Size(131, 28);
            buttonReceive.TabIndex = 1;
            buttonReceive.Text = "Start Server";
            buttonReceive.UseVisualStyleBackColor = true;
            buttonReceive.Click += buttonReceive_Click;
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(79, 124);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(131, 28);
            buttonStop.TabIndex = 2;
            buttonStop.Text = "Stop Server";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(45, 170);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(200, 25);
            progressBar1.TabIndex = 3;
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(45, 198);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(77, 15);
            lblSpeed.TabIndex = 4;
            lblSpeed.Text = "Speed: 0 kB/s";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(280, 280);
            Controls.Add(lblSpeed);
            Controls.Add(progressBar1);
            Controls.Add(buttonStop);
            Controls.Add(buttonReceive);
            Controls.Add(buttonSend);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "MainForm";
            Text = "UTransfer";
            Load += MainForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Button buttonReceive;
        private System.Windows.Forms.Button buttonStop;  // Stop button
        private System.Windows.Forms.ProgressBar progressBar1;  // Progress bar
        private System.Windows.Forms.Label lblSpeed;  // Label to display speed
    }
}
