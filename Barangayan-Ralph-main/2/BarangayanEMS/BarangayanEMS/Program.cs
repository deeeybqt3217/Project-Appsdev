using System;
using System.Windows.Forms;
using BarangayanEMS.Data;

namespace BarangayanEMS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                DatabaseBootstrapper.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to initialize the local database.\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new LoginForm());
        }
    }
}