namespace UTransfer
{
    partial class Form1
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
            buttonEnvoyer = new Button();
            buttonRecevoir = new Button();
            buttonArreter = new Button();
            SuspendLayout();
            // 
            // buttonEnvoyer
            // 
            buttonEnvoyer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonEnvoyer.Location = new Point(79, 75);
            buttonEnvoyer.Name = "buttonEnvoyer";
            buttonEnvoyer.Size = new Size(131, 25);
            buttonEnvoyer.TabIndex = 0;
            buttonEnvoyer.Text = "Envoyer";
            buttonEnvoyer.UseVisualStyleBackColor = true;
            buttonEnvoyer.Click += buttonEnvoyer_Click;
            // 
            // buttonRecevoir
            // 
            buttonRecevoir.Location = new Point(79, 112);
            buttonRecevoir.Name = "buttonRecevoir";
            buttonRecevoir.Size = new Size(131, 28);
            buttonRecevoir.TabIndex = 1;
            buttonRecevoir.Text = "Lancer le Serveur";
            buttonRecevoir.UseVisualStyleBackColor = true;
            buttonRecevoir.Click += buttonRecevoir_Click;
            // 
            // buttonArreter
            // 
            buttonArreter.Location = new Point(79, 150);
            buttonArreter.Name = "buttonArreter";
            buttonArreter.Size = new Size(131, 28);
            buttonArreter.TabIndex = 2;
            buttonArreter.Text = "Arrêter le Serveur";
            buttonArreter.UseVisualStyleBackColor = true;
            buttonArreter.Click += buttonArreter_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(280, 225);
            Controls.Add(buttonArreter);
            Controls.Add(buttonRecevoir);
            Controls.Add(buttonEnvoyer);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "Form1";
            Text = "UTransfer";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        private System.Windows.Forms.Button buttonEnvoyer;
        private System.Windows.Forms.Button buttonRecevoir;
        private System.Windows.Forms.Button buttonArreter;  // Bouton Arrêter
    }
}
