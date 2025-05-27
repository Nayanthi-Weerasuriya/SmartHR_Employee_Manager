using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Employee Dashboard form.
    /// Allows employees to check in/out, view their payroll, and logout.
    /// </summary>
    public class EmployeeDashboard : Form
    {
        // UI controls for check-in/out, payroll, logout, and status display
        private Button btnCheckIn = new Button();
        private Button btnCheckOut = new Button();
        private Button btnPayroll = new Button();
        private Button btnLogout = new Button();
        private Label lblWelcome = new Label();
        private Label lblStatus = new Label();
        private Label lblRate = new Label();

        /// <summary>
        /// Initializes a new instance of the EmployeeDashboard class.
        /// </summary>
        public EmployeeDashboard()
        {
            InitializeComponents();
            UpdateStatus(); // Update initial check-in/out status
        }

        /// <summary>
        /// Initializes the visual components of the employee dashboard.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Smart HR - Employee Dashboard"; // Set form title
            this.Size = new Size(350, 250); // Set form size
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form on screen

            // Welcome label setup
            lblWelcome.Text = $"Welcome, {AuthService.CurrentUser.Name}";
            lblWelcome.Location = new Point(20, 20);
            lblWelcome.AutoSize = true;
            lblWelcome.Font = new Font(lblWelcome.Font, FontStyle.Bold);

            // Hourly rate label setup
            lblRate.Text = $"Hourly Rate: Rs. {AuthService.CurrentUser.SalaryPerHour:N2}";
            lblRate.Location = new Point(20, 50);
            lblRate.AutoSize = true;

            // Check In button setup
            btnCheckIn.Text = "Check In";
            btnCheckIn.Location = new Point(50, 100);
            btnCheckIn.Size = new Size(100, 40);
            btnCheckIn.Click += BtnCheckIn_Click; // Attach click event handler

            // Check Out button setup
            btnCheckOut.Text = "Check Out";
            btnCheckOut.Location = new Point(170, 100);
            btnCheckOut.Size = new Size(100, 40);
            btnCheckOut.Click += BtnCheckOut_Click; // Attach click event handler

            // My Payroll button setup
            btnPayroll.Text = "My Payroll";
            btnPayroll.Location = new Point(50, 150);
            btnPayroll.Size = new Size(220, 40);
            // Event handler to open PayrollReport for the current user
            btnPayroll.Click += (s, e) => new PayrollReport(AuthService.CurrentUser.Id).ShowDialog();

            // Logout button setup
            btnLogout.Text = "Logout";
            btnLogout.Location = new Point(250, 20);
            // Event handler to log out and close the current form
            btnLogout.Click += (s, e) => { AuthService.Logout(); this.Close(); };

            // Add all controls to the form
            this.Controls.AddRange(new Control[] { lblWelcome, lblRate, lblStatus, btnCheckIn, btnCheckOut, btnPayroll, btnLogout });
        }

        /// <summary>
        /// Updates the current check-in/check-out status and enables/disables buttons accordingly.
        /// </summary>
        private void UpdateStatus()
        {
            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
                    SELECT * FROM Attendances
                    WHERE EmployeeId = @employeeId AND CheckOut IS NULL", connection))
                {
                    command.Parameters.AddWithValue("@employeeId", AuthService.CurrentUser.Id);
                    using (var reader = command.ExecuteReader())
                    {
                        bool isCheckedIn = reader.HasRows; // Check if there's an open attendance record
                        lblStatus.Text = isCheckedIn ? "Status: Checked In" : "Status: Checked Out";
                        lblStatus.Location = new Point(20, 80);
                        lblStatus.AutoSize = true;

                        btnCheckIn.Enabled = !isCheckedIn; // Enable Check In if not checked in
                        btnCheckOut.Enabled = isCheckedIn; // Enable Check Out if checked in
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event for the Check In button.
        /// Records a new check-in entry in the database.
        /// </summary>
        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
                    INSERT INTO Attendances (EmployeeId, CheckIn)
                    VALUES (@employeeId, @checkIn)", connection))
                {
                    command.Parameters.AddWithValue("@employeeId", AuthService.CurrentUser.Id);
                    command.Parameters.AddWithValue("@checkIn", DateTime.Now); // Record current time as check-in
                    int result = command.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Checked in successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateStatus(); // Update status after successful check-in
                    }
                    else
                    {
                        MessageBox.Show("Check-in failed. Please try again.", "Check-in Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event for the Check Out button.
        /// Updates the existing check-in entry with a check-out time.
        /// </summary>
        private void BtnCheckOut_Click(object sender, EventArgs e)
        {
            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
                    UPDATE Attendances
                    SET CheckOut = @checkOut
                    WHERE EmployeeId = @employeeId AND CheckOut IS NULL", connection))
                {
                    command.Parameters.AddWithValue("@checkOut", DateTime.Now); // Record current time as check-out
                    command.Parameters.AddWithValue("@employeeId", AuthService.CurrentUser.Id);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Retrieve the updated attendance record to calculate hours worked
                        using (var getCommand = new SQLiteCommand(@"
                            SELECT CheckIn, CheckOut FROM Attendances
                            WHERE EmployeeId = @employeeId AND CheckOut IS NOT NULL
                            ORDER BY CheckOut DESC LIMIT 1", connection))
                        {
                            getCommand.Parameters.AddWithValue("@employeeId", AuthService.CurrentUser.Id);
                            using (var reader = getCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    DateTime checkIn = Convert.ToDateTime(reader["CheckIn"]);
                                    DateTime checkOut = Convert.ToDateTime(reader["CheckOut"]);
                                    TimeSpan duration = checkOut - checkIn;
                                    MessageBox.Show($"Checked out successfully!\nWorked: {duration.TotalHours:F2} hours",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                        UpdateStatus(); // Update status after successful check-out
                    }
                    else
                    {
                        MessageBox.Show("Check-out failed. You might not be checked in.", "Check-out Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
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
