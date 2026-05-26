using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MissionScannerGUI
{
    internal static class Program
    {
        [Flags]
        private enum ErrorModes : uint
        {
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOOPENFILEERRORBOX = 0x8000,
        }

        [DllImport("kernel32.dll")]
        private static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [STAThread]
        static void Main()
        {
            // Suppress the OS "VCRUNTIME140_1D.dll was not found" popup when the
            // backend can't load — we surface our own message based on exit code.
            // Child processes inherit this error mode.
            SetErrorMode(ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOOPENFILEERRORBOX);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new fMain());
        }
    }
}
