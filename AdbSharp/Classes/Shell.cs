using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public static class Shell
    {

        public delegate void OnOutput(string output);
        public static event OnOutput Output;

        public static bool AlwaysShowConsole
        {
            get;
            set;
        }

        public static Task<string> Execute(string path, string arguments = null, int timeout = 0, bool showConsole = false)
        {

            return Task.Run<string>(() =>
            {

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo(path, arguments)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = !showConsole || !AlwaysShowConsole,
                        RedirectStandardOutput = true
                    }
                };

                if (Output != null)
                    Output(string.Format("\n>> {0} {1}\n\n", path, arguments));

                process.Start();

                if (timeout > 0)
                {

                    Task.Run(() =>
                    {

                        Task.Delay(timeout).Wait();

                        if (!process.HasExited)
                            process.Kill();

                    });

                }

                var output = process.StandardOutput.ReadToEnd();

                if (Output != null)
                {
                    Output(output);
                }

                return output;

            });

        }

    }
}
