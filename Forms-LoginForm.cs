using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the login form for the Smart HR application.
    /// </summary>
    public class LoginForm : Form
    {
        // UI controls for username, password, and login button
        private TextBox txtUsername = new TextBox();
        private TextBox txtPassword = new TextBox();
        private Button btnLogin = new Button();
        private Label lblUsername = new Label();
        private Label lblPassword = new Label();

        /// <summary>
        /// Initializes a new instance of the LoginForm class.
        /// </summary>
        public LoginForm()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Initializes the visual components of the login form.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Smart HR - Login"; // Set form title
            this.Size = new Size(300, 200); // Set form size
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Prevent resizing
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form on screen
            this.MaximizeBox = false; // Disable maximize button

            // Username label setup
            lblUsername.Text = "Username:";
            lblUsername.Location = new Point(20, 20);
            lblUsername.AutoSize = true;

            // Username textbox setup
            txtUsername.Location = new Point(100, 20);
            txtUsername.Width = 150;

            // Password label setup
            lblPassword.Text = "Password:";
            lblPassword.Location = new Point(20, 50);
            lblPassword.AutoSize = true;

            // Password textbox setup
            txtPassword.Location = new Point(100, 50);
            txtPassword.Width = 150;
            txtPassword.PasswordChar = '*'; // Mask password input

            // Login button setup
            btnLogin.Text = "Login";
            btnLogin.Location = new Point(100, 90);
            btnLogin.Click += BtnLogin_Click; // Attach click event handler

            // Add all controls to the form
            this.Controls.AddRange(new Control[] { lblUsername, txtUsername, lblPassword, txtPassword, btnLogin });
        }

        /// <summary>
        /// Handles the click event of the login button.
        /// </summary>
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            // Attempt to log in using AuthService
            if (AuthService.Login(txtUsername.Text, txtPassword.Text))
            {
                this.Hide(); // Hide the login form

                // Show appropriate dashboard based on user role
                if (AuthService.IsAdmin())
                {
                    new AdminDashboard().Show();
                }
                else
                {
                    new EmployeeDashboard().Show();
                }
            }
            else
            {
                // Show error message for invalid credentials
                MessageBox.Show("Invalid username or password", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Overrides the OnFormClosed method to ensure the application exits when the login form is closed.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.Exit(); // Exit the entire application
        }
    }
}
