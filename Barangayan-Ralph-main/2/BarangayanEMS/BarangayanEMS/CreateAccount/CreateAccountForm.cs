using System;
using System.Windows.Forms;
using BarangayanEMS.Data;

namespace BarangayanEMS
{
    public partial class CreateAccountForm : Form
    {
        private readonly UserRepository _userRepository = new UserRepository();

        public CreateAccountForm()
        {
            // This calls the Designer version ONLY (do not create another one here)
            InitializeComponent();
        }

        // CREATE ACCOUNT BUTTON CLICK
        private void btnCreate_Click(object sender, EventArgs e)
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string pass = txtPassword.Text;
            string confirm = txtConfirm.Text;

            // Basic required fields check
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(confirm))
            {
                MessageBox.Show(
                    "Please fill in all required fields:\nFirst name, Last name, Email, Password, Confirm Password.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (pass != confirm)
            {
                MessageBox.Show(
                    "Passwords do not match.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!chkTerms.Checked)
            {
                MessageBox.Show(
                    "Please agree to the Terms and Conditions.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_userRepository.TryCreateUser(firstName, lastName, email, pass, out string errorMessage))
            {
                MessageBox.Show(
                    "Account created successfully. You can now log in.",
                    "Create Account",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                var login = new LoginForm();
                login.Show();
                Close();
            }
            else
            {
                MessageBox.Show(
                    errorMessage,
                    "Create Account",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // "Sign in" link – go back to LoginForm
        private void linkSignIn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var login = new LoginForm();
            login.Show();
            Close();
        }
    }
}
