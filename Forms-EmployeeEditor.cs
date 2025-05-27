using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Employee Editor form for adding or editing employee details.
    /// </summary>
    public class EmployeeEditor : Form
    {
        // UI controls for employee details
        private TextBox txtName = new TextBox();
        private TextBox txtUsername = new TextBox();
        private TextBox txtPassword = new TextBox();
        private NumericUpDown numSalary = new NumericUpDown();
        private Button btnSave = new Button();
        private Button btnCancel = new Button();
        private Label[] labels = new Label[4]; // Labels for each input field
        private Employee employee; // Employee object to be edited or added

        /// <summary>
        /// Initializes a new instance of the EmployeeEditor class.
        /// </summary>
        /// <param name="id">Optional employee ID for editing an existing employee. Null for adding a new employee.</param>
        public EmployeeEditor(int? id = null)
        {
            this.Text = id.HasValue ? "Edit Employee" : "Add Employee"; // Set form title based on mode
            this.Size = new Size(350, 250); // Set form size
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Prevent resizing

            // Initialize labels
            string[] labelTexts = { "Name:", "Username:", "Password:", "Salary/Hour (LKR):" };
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = new Label
                {
                    Text = labelTexts[i],
                    Location = new Point(20, 20 + i * 40), // Position labels vertically
                    AutoSize = true
                };
            }

            // Textbox for Name
            txtName.Location = new Point(120, 20);
            txtName.Size = new Size(200, 20);

            // Textbox for Username
            txtUsername.Location = new Point(120, 60);
            txtUsername.Size = new Size(200, 20);

            // Textbox for Password (masked input)
            txtPassword.Location = new Point(120, 100);
            txtPassword.Size = new Size(200, 20);
            txtPassword.PasswordChar = '*';

            // NumericUpDown for Salary Per Hour
            numSalary.Location = new Point(120, 140);
            numSalary.Size = new Size(200, 20);
            numSalary.DecimalPlaces = 2; // Allow two decimal places
            numSalary.Minimum = 0;
            numSalary.Maximum = 10000; // Set a reasonable maximum salary

            // Save button
            btnSave.Text = "Save";
            btnSave.Location = new Point(120, 180);
            btnSave.Size = new Size(80, 30);
            btnSave.Click += BtnSave_Click; // Attach click event handler

            // Cancel button
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(220, 180);
            btnCancel.Size = new Size(80, 30);
            btnCancel.Click += (s, e) => this.Close(); // Close form on cancel

            // Add controls to the form
            this.Controls.AddRange(new Control[] { txtName, txtUsername, txtPassword, numSalary, btnSave, btnCancel });
            this.Controls.AddRange(labels); // Add labels separately

            // Load employee data if an ID is provided (edit mode)
            if (id.HasValue)
            {
                LoadEmployee(id.Value);
            }
            else
            {
                // Initialize a new Employee object for add mode
                employee = new Employee { Role = "Employee" };
            }
        }

        /// <summary>
        /// Loads an existing employee's data into the form fields for editing.
        /// </summary>
        /// <param name="id">The ID of the employee to load.</param>
        private void LoadEmployee(int id)
        {
            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Employees WHERE Id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Populate employee object from database
                            employee = new Employee
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Username = reader["Username"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                SalaryPerHour = Convert.ToDecimal(reader["SalaryPerHour"]),
                                Role = reader["Role"].ToString()
                            };

                            // Populate form fields with employee data
                            txtName.Text = employee.Name;
                            txtUsername.Text = employee.Username;
                            numSalary.Value = employee.SalaryPerHour;
                            // Password field is intentionally left blank for security; user must re-enter to change.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event for the Save button.
        /// Validates input and saves employee data to the database (add or update).
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Name and Username are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update employee object with current form values
            employee.Name = txtName.Text;
            employee.Username = txtUsername.Text;
            employee.SalaryPerHour = numSalary.Value;

            // Only update password hash if a new password is provided
            if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                employee.PasswordHash = AuthService.HashPassword(txtPassword.Text);
            }
            else if (employee.Id == 0) // If adding a new employee, password is mandatory
            {
                MessageBox.Show("Password is required for new employees.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                if (employee.Id == 0) // Add new employee
                {
                    using (var command = new SQLiteCommand(@"
                        INSERT INTO Employees (Name, Username, PasswordHash, SalaryPerHour, Role)
                        VALUES (@name, @username, @passwordHash, @salaryPerHour, @role)", connection))
                    {
                        command.Parameters.AddWithValue("@name", employee.Name);
                        command.Parameters.AddWithValue("@username", employee.Username);
                        command.Parameters.AddWithValue("@passwordHash", employee.PasswordHash);
                        command.Parameters.AddWithValue("@salaryPerHour", employee.SalaryPerHour);
                        command.Parameters.AddWithValue("@role", employee.Role);

                        try
                        {
                            command.ExecuteNonQuery();
                            MessageBox.Show("Employee added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to add employee.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else // Update existing employee
                {
                    // Construct the UPDATE query dynamically based on whether password is being updated
                    string updateQuery = @"
                        UPDATE Employees
                        SET Name = @name, Username = @username, SalaryPerHour = @salaryPerHour";
                    if (!string.IsNullOrEmpty(txtPassword.Text))
                    {
                        updateQuery += ", PasswordHash = @passwordHash";
                    }
                    updateQuery += " WHERE Id = @id";

                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@name", employee.Name);
                        command.Parameters.AddWithValue("@username", employee.Username);
                        command.Parameters.AddWithValue("@salaryPerHour", employee.SalaryPerHour);
                        command.Parameters.AddWithValue("@id", employee.Id);
                        if (!string.IsNullOrEmpty(txtPassword.Text))
                        {
                            command.Parameters.AddWithValue("@passwordHash", employee.PasswordHash);
                        }
                        try
                        {
                            command.ExecuteNonQuery();
                            MessageBox.Show("Employee updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to update employee.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            this.DialogResult = DialogResult.OK; // Set dialog result to OK
            this.Close(); // Close the form
        }
    }
}
