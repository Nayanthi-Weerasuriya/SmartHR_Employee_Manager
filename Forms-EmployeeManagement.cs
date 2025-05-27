using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// Represents the Employee Management form, allowing admins to view, add, edit, and delete employee records.
    /// </summary>
    public class EmployeeManagement : Form
    {
        // UI controls for displaying employees and managing them
        private DataGridView dgvEmployees = new DataGridView();
        private Button btnAdd = new Button();
        private Button btnEdit = new Button();
        private Button btnDelete = new Button();
        private Button btnClose = new Button();

        /// <summary>
        /// Initializes a new instance of the EmployeeManagement class.
        /// </summary>
        public EmployeeManagement()
        {
            InitializeComponents();
            LoadEmployees(); // Load employee data when the form is initialized
        }

        /// <summary>
        /// Initializes the visual components of the employee management form.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Employee Management"; // Set form title
            this.Size = new Size(600, 400); // Set form size
            this.StartPosition = FormStartPosition.CenterScreen; // Center the form on screen

            // DataGridView setup for displaying employee data
            dgvEmployees.Location = new Point(20, 20);
            dgvEmployees.Size = new Size(550, 250);
            dgvEmployees.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // Select full row
            dgvEmployees.ReadOnly = true; // Make grid read-only
            dgvEmployees.AllowUserToAddRows = false; // Prevent adding rows directly in the grid
            dgvEmployees.AutoGenerateColumns = false; // Manually define columns

            // Define columns for the DataGridView
            dgvEmployees.Columns.Add("Id", "ID");
            dgvEmployees.Columns.Add("Name", "Name");
            dgvEmployees.Columns.Add("Username", "Username");
            dgvEmployees.Columns.Add("SalaryPerHour", "Salary/Hour (LKR)");
            dgvEmployees.Columns["SalaryPerHour"].DefaultCellStyle.Format = "N2"; // Format salary as currency

            // Add button setup
            btnAdd.Text = "Add";
            btnAdd.Location = new Point(20, 290);
            btnAdd.Size = new Size(100, 30);
            // Event handler to open EmployeeEditor for adding a new employee, then reload data
            btnAdd.Click += (s, e) => { new EmployeeEditor().ShowDialog(); LoadEmployees(); };

            // Edit button setup
            btnEdit.Text = "Edit";
            btnEdit.Location = new Point(130, 290);
            btnEdit.Size = new Size(100, 30);
            btnEdit.Click += (s, e) => EditEmployee(); // Attach click event handler

            // Delete button setup
            btnDelete.Text = "Delete";
            btnDelete.Location = new Point(240, 290);
            btnDelete.Size = new Size(100, 30);
            btnDelete.Click += (s, e) => DeleteEmployee(); // Attach click event handler

            // Close button setup
            btnClose.Text = "Close";
            btnClose.Location = new Point(470, 290);
            btnClose.Size = new Size(100, 30);
            btnClose.Click += (s, e) => this.Close(); // Close the form

            // Add all controls to the form
            this.Controls.AddRange(new Control[] { dgvEmployees, btnAdd, btnEdit, btnDelete, btnClose });
        }

        /// <summary>
        /// Loads employee data from the database into the DataGridView.
        /// Only loads employees with the 'Employee' role.
        /// </summary>
        private void LoadEmployees()
        {
            dgvEmployees.Rows.Clear(); // Clear existing rows

            using (var connection = new SQLiteConnection(AuthService.ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Employees WHERE Role = 'Employee'", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Add employee data to the DataGridView
                            dgvEmployees.Rows.Add(
                                Convert.ToInt32(reader["Id"]),
                                reader["Name"].ToString(),
                                reader["Username"].ToString(),
                                Convert.ToDecimal(reader["SalaryPerHour"])
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Opens the EmployeeEditor form to edit the selected employee.
        /// </summary>
        private void EditEmployee()
        {
            if (dgvEmployees.SelectedRows.Count == 0) return; // Do nothing if no row is selected

            // Get the ID of the selected employee
            int id = (int)dgvEmployees.SelectedRows[0].Cells["Id"].Value;
            new EmployeeEditor(id).ShowDialog(); // Open editor with selected employee's ID
            LoadEmployees(); // Reload data after editing
        }

        /// <summary>
        /// Deletes the selected employee from the database after confirmation.
        /// </summary>
        private void DeleteEmployee()
        {
            if (dgvEmployees.SelectedRows.Count == 0) return; // Do nothing if no row is selected

            // Get ID and name of the selected employee
            int id = (int)dgvEmployees.SelectedRows[0].Cells["Id"].Value;
            string name = dgvEmployees.SelectedRows[0].Cells["Name"].Value.ToString();

            // Ask for confirmation before deleting
            if (MessageBox.Show($"Are you sure you want to delete {name}?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (var connection = new SQLiteConnection(AuthService.ConnectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("DELETE FROM Employees WHERE Id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        try
                        {
                            command.ExecuteNonQuery();
                            MessageBox.Show("Employee deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to delete employee.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                LoadEmployees(); // Reload data after deletion
            }
        }
    }
}
