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
            buttonStop = new Button();
            SuspendLayout();
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.Location = new Point(56, 30);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(171, 15);
            labelMessage.TabIndex = 0;
            labelMessage.Text = "Server waiting for connection...";
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(90, 70);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(100, 30);
            buttonStop.TabIndex = 1;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // ServerConnectionForm
            // 
            ClientSize = new Size(284, 131);
            Controls.Add(buttonStop);
            Controls.Add(labelMessage);
            Name = "ServerConnectionForm";
            Text = "Server Connection";
            Load += ServerConnectionForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.Button buttonStop;
    }
}
//for commit