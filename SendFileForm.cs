using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace UTransfer
{
    public partial class SendFileForm : Form
    {
        private bool isCancelled = false;
        private Dictionary<string, string> savedContacts = new Dictionary<string, string>();

        public SendFileForm()
        {
            InitializeComponent();
        }

        private void SendFileForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("SendFileForm window loaded.");

            LoadSavedContacts();
            UpdateContactsComboBox();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                string ipAddress = txtIpAddress.Text;  // Retrieve the entered IP address
                List<string> filePaths = SelectFiles();  // Open a dialog to select files

                if (!string.IsNullOrEmpty(ipAddress) && filePaths.Count > 0)
                {
                    Debug.WriteLine($"Sending files to {ipAddress}");

                    // Save the contact if the checkbox is checked
                    if (chkSaveForLater.Checked)
                    {
                        string username = txtUsername.Text;
                        if (string.IsNullOrEmpty(username))
                        {
                            MessageBox.Show("Please enter a username to save the contact.");
                            return;
                        }

                        // Add or update the contact
                        savedContacts[username] = ipAddress;
                        SaveContacts();
                        UpdateContactsComboBox();
                    }

                    // Ask the user to enter a passphrase
                    string? passphrase = PromptForPassphrase();
                    if (passphrase == null)
                    {
                        // The user canceled the input
                        MessageBox.Show("Operation canceled. A passphrase is required to send files.");
                        return;
                    }

                    // Reset the progress bar before sending
                    progressBar.Value = 0;
                    lblSpeed.Text = "Speed: 0 kB/s";
                    isCancelled = false;

                    // Use the SendFiles method to send multiple files sequentially
                    NetworkHelper.SendFiles(ipAddress, filePaths, progressBar, lblSpeed, () => isCancelled, passphrase);
                }
                else
                {
                    MessageBox.Show("Please enter a valid IP address and select at least one file.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending files: {ex.Message}");
                MessageBox.Show($"Error sending files: {ex.Message}");
            }
        }

        // Method to open a dialog to select multiple files
        private List<string> SelectFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true  // Allow multiple file selection
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return new List<string>(openFileDialog.FileNames);  // Return the selected files' paths
            }
            return new List<string>();
        }

        // Method to cancel the sending process
        private void btnCancel_Click(object sender, EventArgs e)
        {
            isCancelled = true;  // Mark the transfer as canceled
            MessageBox.Show("Sending canceled.");
        }

        // Method to prompt the user for a passphrase
        private string? PromptForPassphrase()
        {
            using (PassphraseForm passphraseForm = new PassphraseForm())
            {
                if (passphraseForm.ShowDialog() == DialogResult.OK)
                {
                    return passphraseForm.Passphrase;
                }
                else
                {
                    return null;
                }
            }
        }

        // Method to load saved contacts
        private void LoadSavedContacts()
        {
            try
            {
                string contactsFilePath = GetContactsFilePath();
                if (File.Exists(contactsFilePath))
                {
                    string json = File.ReadAllText(contactsFilePath);
                    savedContacts = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading contacts: {ex.Message}");
                savedContacts = new Dictionary<string, string>();
            }
        }

        // Method to save contacts
        private void SaveContacts()
        {
            try
            {
                string contactsFilePath = GetContactsFilePath();
                string json = JsonConvert.SerializeObject(savedContacts, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(contactsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving contacts: {ex.Message}");
            }
        }

        // Method to get the contacts file path
        private string GetContactsFilePath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string uTransferFolder = Path.Combine(appDataFolder, "UTransfer");
            Directory.CreateDirectory(uTransferFolder);
            return Path.Combine(uTransferFolder, "contacts.json");
        }

        // Update the contacts ComboBox
        private void UpdateContactsComboBox()
        {
            cmbSavedContacts.Items.Clear();
            foreach (var contact in savedContacts)
            {
                cmbSavedContacts.Items.Add(contact.Key);
            }
        }

        // Event when the checkbox is checked or unchecked
        private void chkSaveForLater_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = chkSaveForLater.Checked;
            lblUsername.Visible = isChecked;
            txtUsername.Visible = isChecked;
        }

        // Event when the user selects a contact from the ComboBox
        private void cmbSavedContacts_SelectedIndexChanged(object sender, EventArgs e)
        {
            string? selectedName = cmbSavedContacts.SelectedItem as string;
            if (selectedName != null && savedContacts.ContainsKey(selectedName))
            {
                txtIpAddress.Text = savedContacts[selectedName];
            }
        }
    }
}
