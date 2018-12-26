using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Debugger
{
    public static class Program
    {
        private static string _namespace;
        internal static string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(_namespace))
                    _namespace = typeof(Program).Namespace;
                return _namespace;
            }
        }

        private static Version _version;
        internal static Version Version
        {
            get
            {
                if (_version == null)
                    _version = Assembly.GetExecutingAssembly().GetName().Version;
                return _version;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            try
            {
                Logging.LogManager.Initialize();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to initialized logging");
                Console.WriteLine(ex);
                return;
            }

            _logger.Info($"{Namespace} v{Version} intitialized");

            if (args.Length == 0)
            {
                _logger.Warn("No comamnd line parameters specified. Aborting...");
                return;
            }

            var processName = args[0];
            if (processName.ToLower().EndsWith(".exe"))
                processName = processName.Substring(0, processName.Length - 4);
            _logger.Info($"Target process name: {processName}");

            var processes = Process.GetProcessesByName(processName);
            _logger.Info($"Found {processes.Length} processes with name {processName}");
            foreach(var process in processes)
            {
                try
                {
                    _logger.Info($"Trying to close [{process.Id}]{process.ProcessName} gracefully");
                    // Send a WM_CLOSE and wait for a gracefull exit
                    PostMessage(process.Handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    if (!process.WaitForExit(1000))
                    {
                        _logger.Info($"Process did not close within 1 second. Killing {process.Id}");
                        process.Kill();
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, $"Error while trying to process [{process.Id}]{process.ProcessName}");
                }
            }
            _logger.Info("Program ended successfully");
        }
    }
}
