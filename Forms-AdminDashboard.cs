using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Admin Dashboard form.
    /// Provides navigation to employee management, payroll reports, and attendance view.
    /// </summary>
    public class AdminDashboard : Form
    {
        // UI controls for navigation and welcome message
        private Button btnEmployees = new Button();
        private Button btnPayroll = new Button();
        private Button btnAttendance = new Button();
        private Button btnLogout = new Button();
        private Label lblWelcome = new Label();

        /// <summary>
        /// Initializes a new instance of the AdminDashboard class.
        /// </summary>
        public AdminDashboard()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Initializes the visual components of the admin dashboard.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Smart HR - Admin Dashboard"; // Set form title
            this.Size = new Size(400, 300); // Set form size
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form on screen

            // Welcome label setup, displaying the current user's name
            lblWelcome.Text = $"Welcome, {AuthService.CurrentUser.Name}";
            lblWelcome.Location = new Point(20, 20);
            lblWelcome.AutoSize = true;
            lblWelcome.Font = new Font(lblWelcome.Font, FontStyle.Bold); // Bold font for welcome message

            // Manage Employees button setup
            btnEmployees.Text = "Manage Employees";
            btnEmployees.Location = new Point(50, 70);
            btnEmployees.Size = new Size(200, 40);
            // Event handler to open EmployeeManagement form as a dialog
            btnEmployees.Click += (s, e) => new EmployeeManagement().ShowDialog();

            // Payroll Reports button setup
            btnPayroll.Text = "Payroll Reports";
            btnPayroll.Location = new Point(50, 120);
            btnPayroll.Size = new Size(200, 40);
            // Event handler to open PayrollReport form as a dialog
            btnPayroll.Click += (s, e) => new PayrollReport().ShowDialog();

            // View Attendance button setup
            btnAttendance.Text = "View Attendance";
            btnAttendance.Location = new Point(50, 170);
            btnAttendance.Size = new Size(200, 40);
            // Event handler to open AttendanceView form as a dialog
            btnAttendance.Click += (s, e) => new AttendanceView().ShowDialog();

            // Logout button setup
            btnLogout.Text = "Logout";
            btnLogout.Location = new Point(270, 20);
            // Event handler to log out and close the current form
            btnLogout.Click += (s, e) => { AuthService.Logout(); this.Close(); };

            // Add all controls to the form
            this.Controls.AddRange(new Control[] { lblWelcome, btnEmployees, btnPayroll, btnAttendance, btnLogout });
        }

        /// <summary>
        /// Overrides the OnFormClosed method to handle application exit logic.
        /// If no other forms are open, the application will exit.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            // If this is the last open form, exit the application
            if (Application.OpenForms.Count == 0) Application.Exit();
        }
    }
}
