using System;
using System.Windows.Forms;

namespace SmartHR
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize the database and create admin user if not exists
            AuthService.InitializeDatabase();

            Application.Run(new LoginForm());
        }
    }
}
