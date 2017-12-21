using System;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace Leer_Copy
{
    static class Program
    {
        /// <summary>
        /// Author: Weston Berg (weberg@iastate.edu)
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(ThreadHandler);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(DomainExceptionHandler);
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                CopyWindowFrm frm = new CopyWindowFrm();
                Application.Run(frm);
                if (frm.NeedsRestart)
                {
                    Application.Restart();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error. Check logs at:\n\n" + Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Leer Copy\\log.txt"
                    + "\n\nfor more information on the error.", "SEVERE");
                LogError(ex.Message);
            }
        }

        static void ThreadHandler(object sender, ThreadExceptionEventArgs e)
        {
            LogError(e.Exception.Message);
        }

        static void DomainExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LogError(e.ToString());
        }

        private static void LogError(string str)
        {
            using (StreamWriter w = File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy/log.txt"))
            {
                w.Write("\r\nLog Entry : ERROR");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", str);
                w.WriteLine("-------------------------------");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
