using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Attendance View form, allowing users to view attendance records within a specified date range.
    /// </summary>
    public class AttendanceView : Form
    {
        // UI controls for displaying attendance data and filtering
        private DataGridView dgvAttendances = new DataGridView();
        private Button btnClose = new Button();
        private DateTimePicker dtpFrom = new DateTimePicker();
        private DateTimePicker dtpTo = new DateTimePicker();
        private Label lblFrom = new Label();
        private Label lblTo = new Label();
        private Button btnFilter = new Button();

        /// <summary>
        /// Initializes a new instance of the AttendanceView class.
        /// </summary>
        public AttendanceView()
        {
            InitializeComponents();
            LoadData(); // Load initial attendance data
        }

        /// <summary>
        /// Initializes the visual components of the attendance view form.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Attendance Records"; // Set form title
            this.Size = new Size(700, 400); // Set form size
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form

            // "From" date picker and label
            lblFrom.Text = "From:";
            lblFrom.Location = new Point(20, 20);
            lblFrom.AutoSize = true;

            dtpFrom.Location = new Point(70, 20);
            dtpFrom.Size = new Size(150, 20);
            dtpFrom.Format = DateTimePickerFormat.Short;

            // "To" date picker and label
            lblTo.Text = "To:";
            lblTo.Location = new Point(240, 20);
            lblTo.AutoSize = true;

            dtpTo.Location = new Point(270, 20);
            dtpTo.Size = new Size(150, 20);
            dtpTo.Format = DateTimePickerFormat.Short;

            // Filter button
            btnFilter.Text = "Filter";
            btnFilter.Location = new Point(440, 20);
            btnFilter.Size = new Size(100, 20);
            btnFilter.Click += (s, e) => LoadData(); // Reload data on filter click

            // DataGridView for displaying attendance records
            dgvAttendances.Location = new Point(20, 50);
            dgvAttendances.Size = new Size(650, 250);
            dgvAttendances.ReadOnly = true;
            dgvAttendances.AllowUserToAddRows = false;
            dgvAttendances.AutoGenerateColumns = false; // Manually define columns

            // Define columns for the DataGridView
            dgvAttendances.Columns.Add("EmployeeName", "Employee");
            dgvAttendances.Columns.Add("CheckIn", "Check In");
            dgvAttendances.Columns.Add("CheckOut", "Check Out");
            dgvAttendances.Columns.Add("Hours", "Hours Worked");

            // Close button
            btnClose.Text = "Close";
            btnClose.Location = new Point(570, 310);
            btnClose.Size = new Size(100, 30);
            btnClose.Click += (s, e) => this.Close();

            // Add all controls to the form
            this.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnFilter, dgvAttendances, btnClose });
        }

        /// <summary>
        /// Loads attendance data from the database into the DataGridView based on the selected date range.
        /// Joins with the Employees table to display employee names.
        /// </summary>
        private void LoadData()
        {
            dgvAttendances.Rows.Clear(); // Clear existing rows

            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
                    SELECT a.Id, a.CheckIn, a.CheckOut, e.Name AS EmployeeName
                    FROM Attendances a
                    JOIN Employees e ON a.EmployeeId = e.Id
                    WHERE a.CheckIn >= @fromDate AND a.CheckIn <= @toDate
                    ORDER BY a.CheckIn DESC", connection))
                {
                    // Add parameters for date range filter
                    command.Parameters.AddWithValue("@fromDate", dtpFrom.Value.Date);
                    command.Parameters.AddWithValue("@toDate", dtpTo.Value.Date.AddDays(1)); // Include the entire 'to' day

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime checkIn = Convert.ToDateTime(reader["CheckIn"]);
                            object checkOutObj = reader["CheckOut"];
                            // Handle nullable CheckOut column
                            DateTime? checkOut = checkOutObj == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(checkOutObj);

                            // Calculate hours worked if CheckOut is available
                            string hoursWorked = checkOut.HasValue ? (checkOut.Value - checkIn).TotalHours.ToString("F2") : "N/A";

                            // Add row to DataGridView
                            dgvAttendances.Rows.Add(
                                reader["EmployeeName"].ToString(),
                                checkIn,
                                checkOut,
                                hoursWorked
                            );
                        }
                    }
                }
            }
        }
    }
}
