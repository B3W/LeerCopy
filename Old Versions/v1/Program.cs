// Copyright 2017 Weston Berg (westieberg@gmail.com)
//
// This file is part of Leer Copy.
//
// Leer Copy is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Leer Copy is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Leer Copy.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace Leer_Copy
{
    static class Program
    {
        /// <summary>
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
