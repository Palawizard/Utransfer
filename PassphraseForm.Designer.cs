namespace UTransfer
{
    partial class PassphraseForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtPassphrase;
        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

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
            txtPassphrase = new TextBox();
            lblPrompt = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // txtPassphrase
            // 
            txtPassphrase.Location = new Point(12, 34);
            txtPassphrase.Name = "txtPassphrase";
            txtPassphrase.Size = new Size(260, 23);
            txtPassphrase.TabIndex = 0;
            txtPassphrase.UseSystemPasswordChar = true;
            // 
            // lblPrompt
            // 
            lblPrompt.AutoSize = true;
            lblPrompt.Location = new Point(9, 9);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new Size(99, 15);
            lblPrompt.TabIndex = 1;
            lblPrompt.Text = "Enter a password:";
            lblPrompt.Click += lblPrompt_Click;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(116, 60);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(197, 60);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // PassphraseForm
            // 
            ClientSize = new Size(284, 95);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(lblPrompt);
            Controls.Add(txtPassphrase);
            Name = "PassphraseForm";
            Text = "Enter Password";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
