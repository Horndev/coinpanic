using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xwt;

namespace CoinpanicSignUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Xwt.Application.Initialize(ToolkitType.Gtk);
            var mainWindow = new Window()
            {
                Title = "Xwt Demo Application",
                Width = 500,
                Height = 400
            };
            mainWindow.Show();
            Xwt.Application.Run();
            mainWindow.Dispose();
        }
    }
}
