using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmartHR
{
    /// <summary>
    /// Provides authentication and database interaction services.
    /// This class handles user login, password hashing, and database initialization.
    /// </summary>
    public static class AuthService
    {
        // Stores the currently logged-in user.
        public static Employee CurrentUser { get; private set; }

        // Defines the path for the SQLite database file.
        private static readonly string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartHR", "smartHR.db");

        // Provides the connection string for the SQLite database.
        public static string ConnectionString => $"Data Source={dbPath};Version=3;";

        /// <summary>
        /// Attempts to log in a user with the provided username and password.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>True if login is successful, false otherwise.</returns>
        public static bool Login(string username, string password)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM Employees WHERE Username = @username", connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Create an Employee object from the database row
                            var employee = new Employee
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Username = reader["Username"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                SalaryPerHour = Convert.ToDecimal(reader["SalaryPerHour"]),
                                Role = reader["Role"].ToString()
                            };

                            // Verify the entered password against the stored hash
                            if (VerifyPassword(password, employee.PasswordHash))
                            {
                                CurrentUser = employee; // Set the current user
                                return true;
                            }
                        }
                    }
                }
            }
            return false; // Login failed
        }

        /// <summary>
        /// Hashes a given password using SHA256 algorithm.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The SHA256 hash of the password as a hexadecimal string.</returns>
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // Convert byte array to hexadecimal string
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Verifies if an entered password matches a stored hashed password.
        /// </summary>
        /// <param name="enteredPassword">The password entered by the user.</param>
        /// <param name="storedPasswordHash">The hashed password stored in the database.</param>
        /// <returns>True if passwords match, false otherwise.</returns>
        private static bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            return HashPassword(enteredPassword) == storedPasswordHash;
        }

        /// <summary>
        /// Logs out the current user by setting CurrentUser to null.
        /// </summary>
        public static void Logout() => CurrentUser = null;

        /// <summary>
        /// Checks if the current logged-in user has the 'Admin' role.
        /// </summary>
        /// <returns>True if the current user is an Admin, false otherwise.</returns>
        public static bool IsAdmin() => CurrentUser?.Role == "Admin";

        /// <summary>
        /// Initializes the SQLite database, creating tables if they don't exist
        /// and adding a default admin user if one is not present.
        /// </summary>
        public static void InitializeDatabase()
        {
            // Ensure the directory for the database exists
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Create Employees table if it doesn't exist
                using (var command = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Employees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        SalaryPerHour DECIMAL NOT NULL,
                        Role TEXT NOT NULL DEFAULT 'Employee'
                    )", connection))
                {
                    command.ExecuteNonQuery();
                }

                // Create Attendances table if it doesn't exist
                using (var command = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Attendances (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EmployeeId INTEGER NOT NULL,
                        CheckIn DATETIME NOT NULL,
                        CheckOut DATETIME,
                        FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
                    )", connection))
                {
                    command.ExecuteNonQuery();
                }

                // Check if an admin user already exists.
                // If not, create a default admin user.
                using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Employees WHERE Username = 'admin'", connection))
                {
                    var count = Convert.ToInt64(command.ExecuteScalar());
                    if (count == 0)
                    {
                        using (var insertCommand = new SQLiteCommand(@"
                            INSERT INTO Employees (Name, Username, PasswordHash, SalaryPerHour, Role)
                            VALUES ('Admin', 'admin', @password, 0, 'Admin')", connection))
                        {
                            insertCommand.Parameters.AddWithValue("@password", HashPassword("admin123")); // Default admin password
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
