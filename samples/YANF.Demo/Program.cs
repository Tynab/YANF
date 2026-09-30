using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YANF.Demo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if NET
            // .NET: PerMonitorV2 (ApplicationHighDpiMode in YANF.Demo.csproj), visual styles and GDI text rendering, before any window
            ApplicationConfiguration.Initialize();
#else
            // .NET Framework: the DPI awareness comes from app.manifest and App.config
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            Application.Run(new MainFrm());
        }
    }
}
