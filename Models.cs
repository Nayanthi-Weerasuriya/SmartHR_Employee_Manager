using System;

namespace SmartHR
{
    /// <summary>
    /// Represents an Employee in the system.
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public decimal SalaryPerHour { get; set; }
        public string Role { get; set; } = "Employee"; // Default role is Employee
    }

    /// <summary>
    /// Represents an Attendance record for an employee.
    /// </summary>
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; } // Nullable DateTime for when an employee is still checked in
    }
}
