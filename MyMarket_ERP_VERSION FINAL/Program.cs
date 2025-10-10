using System;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Database.EnsureInitialized();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Usar ApplicationContext para mantener la app viva
            Application.Run(new AppContext());
        }
    }

    /// <summary>
    /// Contexto de aplicaci�n que mantiene la app ejecut�ndose
    /// mientras haya formularios abiertos
    /// </summary>
    internal class AppContext : ApplicationContext
    {
        public AppContext()
        {
            // Mostrar el Login inicial
            ShowLogin();
        }

        private void ShowLogin()
        {
            var login = new Login();
            login.FormClosed += Login_FormClosed;
            login.Show();
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Si no hay m�s formularios abiertos, cerrar la aplicaci�n
            if (Application.OpenForms.Count == 0)
            {
                ExitThread(); // Cierra la aplicaci�n
            }
        }
    }
}