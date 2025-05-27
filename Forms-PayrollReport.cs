using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Payroll Report form.
    /// Can display payroll for a single employee or a report for all employees within a date range.
    /// </summary>
    public class PayrollReport : Form
    {
        // UI controls for displaying payroll data and filtering
        private DataGridView dgvPayroll = new DataGridView();
        private Button btnClose = new Button();
        private Button btnExport = new Button(); // Only visible for admin view
        private DateTimePicker dtpFrom = new DateTimePicker();
        private DateTimePicker dtpTo = new DateTimePicker();
        private Label lblFrom = new Label();
        private Label lblTo = new Label();
        private Button btnFilter = new Button(); // Only visible for admin view
        private int? employeeId; // Null for admin view (all employees), contains ID for employee's own view

        /// <summary>
        /// Initializes a new instance of the PayrollReport class.
        /// </summary>
        /// <param name="empId">Optional employee ID. If provided, shows payroll for that specific employee.</param>
        public PayrollReport(int? empId = null)
        {
            employeeId = empId;
            this.Text = empId.HasValue ? "My Payroll" : "Payroll Report"; // Set form title based on mode
            this.Size = new Size(600, empId.HasValue ? 300 : 400); // Adjust size based on mode
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form

            // If no employee ID is provided, this is the admin view, so show date filters
            if (!empId.HasValue)
            {
                lblFrom.Text = "From:";
                lblFrom.Location = new Point(20, 20);
                lblFrom.AutoSize = true;

                dtpFrom.Location = new Point(70, 20);
                dtpFrom.Size = new Size(150, 20);
                dtpFrom.Format = DateTimePickerFormat.Short;

                lblTo.Text = "To:";
                lblTo.Location = new Point(240, 20);
                lblTo.AutoSize = true;

                dtpTo.Location = new Point(270, 20);
                dtpTo.Size = new Size(150, 20);
                dtpTo.Format = DateTimePickerFormat.Short;

                btnFilter.Text = "Filter";
                btnFilter.Location = new Point(440, 20);
                btnFilter.Size = new Size(100, 20);
                btnFilter.Click += (s, e) => LoadData(); // Reload data on filter click

                this.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnFilter });
            }

            // DataGridView setup
            dgvPayroll.Location = new Point(20, empId.HasValue ? 20 : 50); // Adjust position based on date filters presence
            dgvPayroll.Size = new Size(550, empId.HasValue ? 200 : 250); // Adjust size based on date filters presence
            dgvPayroll.ReadOnly = true;
            dgvPayroll.AllowUserToAddRows = false;
            dgvPayroll.AutoGenerateColumns = true; // Allow auto-generation for dynamic columns

            // Close button setup
            btnClose.Text = "Close";
            btnClose.Location = new Point(470, empId.HasValue ? 230 : 310); // Adjust position based on date filters presence
            btnClose.Size = new Size(100, 30);
            btnClose.Click += (s, e) => this.Close();

            // Export button (only for admin view)
            if (!empId.HasValue)
            {
                btnExport.Text = "Export to CSV";
                btnExport.Location = new Point(350, 310);
                btnExport.Size = new Size(100, 30);
                btnExport.Click += BtnExport_Click;
                this.Controls.Add(btnExport);
            }

            this.Controls.AddRange(new Control[] { dgvPayroll, btnClose });
            LoadData(); // Load initial data
        }

        /// <summary>
        /// Loads payroll data into the DataGridView based on the current mode (single employee or all employees).
        /// </summary>
        private void LoadData()
        {
            dgvPayroll.DataSource = null; // Clear existing data source

            if (employeeId.HasValue) // Single employee payroll view
            {
                using (var connection = new SQLiteConnection(AuthService.ConnectionString))
                {
                    connection.Open();

                    // Retrieve employee details
                    Employee employee = null;
                    using (var command = new SQLiteCommand("SELECT * FROM Employees WHERE Id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", employeeId.Value);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                employee = new Employee
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Username = reader["Username"].ToString(),
                                    PasswordHash = reader["PasswordHash"].ToString(),
                                    SalaryPerHour = Convert.ToDecimal(reader["SalaryPerHour"]),
                                    Role = reader["Role"].ToString()
                                };
                            }
                        }
                    }

                    if (employee == null)
                    {
                        MessageBox.Show("Employee record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Calculate total hours worked for the employee
                    double totalHours = 0;
                    using (var command = new SQLiteCommand(@"
                        SELECT CheckIn, CheckOut FROM Attendances
                        WHERE EmployeeId = @employeeId AND CheckOut IS NOT NULL", connection))
                    {
                        command.Parameters.AddWithValue("@employeeId", employeeId.Value);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime checkIn = Convert.ToDateTime(reader["CheckIn"]);
                                DateTime checkOut = Convert.ToDateTime(reader["CheckOut"]);
                                totalHours += (checkOut - checkIn).TotalHours;
                            }
                        }
                    }

                    if (totalHours == 0)
                    {
                        MessageBox.Show("No attendance data found for the selected period.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    // Calculate payroll components
                    decimal grossSalary = Math.Round(employee.SalaryPerHour * (decimal)totalHours, 2);
                    decimal tax = Math.Round(grossSalary * 0.10m, 2); // 10% tax
                    decimal netSalary = grossSalary - tax;

                    // Create a DataTable to display payroll details
                    var table = new DataTable();
                    table.Columns.Add("Description");
                    table.Columns.Add("Amount (LKR)", typeof(decimal));

                    table.Rows.Add("Hourly Rate", employee.SalaryPerHour);
                    table.Rows.Add("Gross Salary", grossSalary);
                    table.Rows.Add("Tax (10%)", tax);
                    table.Rows.Add("Net Salary", netSalary);

                    dgvPayroll.DataSource = table;
                    dgvPayroll.Columns["Amount (LKR)"].DefaultCellStyle.Format = "N2"; // Format amounts
                }
            }
            else // Admin view (all employees payroll)
            {
                using (var connection = new SQLiteConnection(AuthService.ConnectionString))
                {
                    connection.Open();
                    var table = new DataTable();
                    table.Columns.Add("ID", typeof(int));
                    table.Columns.Add("Name");
                    table.Columns.Add("Gross Salary (LKR)", typeof(decimal));
                    table.Columns.Add("Tax (10%) (LKR)", typeof(decimal));
                    table.Columns.Add("Net Salary (LKR)", typeof(decimal));

                    // Get all employees with 'Employee' role
                    using (var empCommand = new SQLiteCommand("SELECT * FROM Employees WHERE Role = 'Employee'", connection))
                    {
                        using (var empReader = empCommand.ExecuteReader())
                        {
                            while (empReader.Read())
                            {
                                int empId = Convert.ToInt32(empReader["Id"]);
                                string name = empReader["Name"].ToString();
                                decimal salaryPerHour = Convert.ToDecimal(empReader["SalaryPerHour"]);

                                // Calculate total hours worked for each employee within the selected date range
                                double totalHours = 0;
                                using (var attCommand = new SQLiteCommand(@"
                                    SELECT CheckIn, CheckOut FROM Attendances
                                    WHERE EmployeeId = @employeeId
                                    AND CheckOut IS NOT NULL
                                    AND CheckIn >= @fromDate
                                    AND CheckIn <= @toDate", connection))
                                {
                                    attCommand.Parameters.AddWithValue("@employeeId", empId);
                                    attCommand.Parameters.AddWithValue("@fromDate", dtpFrom.Value.Date);
                                    attCommand.Parameters.AddWithValue("@toDate", dtpTo.Value.Date.AddDays(1)); // Include full 'to' day

                                    using (var attReader = attCommand.ExecuteReader())
                                    {
                                        while (attReader.Read())
                                        {
                                            DateTime checkIn = Convert.ToDateTime(attReader["CheckIn"]);
                                            DateTime checkOut = Convert.ToDateTime(attReader["CheckOut"]);
                                            totalHours += (checkOut - checkIn).TotalHours;
                                        }
                                    }
                                }

                                // Calculate payroll for each employee
                                decimal grossSalary = Math.Round(salaryPerHour * (decimal)totalHours, 2);
                                decimal tax = Math.Round(grossSalary * 0.10m, 2);
                                decimal netSalary = grossSalary - tax;

                                table.Rows.Add(empId, name, grossSalary, tax, netSalary);
                            }
                        }
                    }

                    dgvPayroll.DataSource = table;
                    // Format currency columns
                    dgvPayroll.Columns["Gross Salary (LKR)"].DefaultCellStyle.Format = "N2";
                    dgvPayroll.Columns["Tax (10%) (LKR)"].DefaultCellStyle.Format = "N2";
                    dgvPayroll.Columns["Net Salary (LKR)"].DefaultCellStyle.Format = "N2";
                }
            }
        }

        /// <summary>
        /// Handles the click event for the Export to CSV button.
        /// Exports the current payroll data displayed in the DataGridView to a CSV file.
        /// </summary>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "CSV files (*.csv)|*.csv", // Filter for CSV files
                FileName = $"Payroll_{DateTime.Now:yyyyMMdd}.csv" // Default filename
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    // Add CSV header
                    sb.AppendLine("ID,Name,Gross Salary (LKR),Tax (10%) (LKR),Net Salary (LKR)");

                    // Iterate through DataGridView rows and append to StringBuilder
                    foreach (DataGridViewRow row in dgvPayroll.Rows)
                    {
                        if (!row.IsNewRow) // Skip the new row placeholder
                        {
                            sb.AppendLine($"{row.Cells["ID"].Value}," +
                                $"{row.Cells["Name"].Value}," +
                                $"{row.Cells["Gross Salary (LKR)"].Value}," +
                                $"{row.Cells["Tax (10%) (LKR)"].Value}," +
                                $"{row.Cells["Net Salary (LKR)"].Value}");
                        }
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString()); // Write content to file
                    MessageBox.Show("Export completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
