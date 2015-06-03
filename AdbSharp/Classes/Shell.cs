using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public static class Shell
    {

        public delegate void OnOutput(string output);
        public static event OnOutput Output;

        public delegate void OnCommand(string command);
        public static event OnCommand Command;

        public delegate void OnException(Exception exception);
        public static event OnException Exception;

        public static bool AlwaysCreateWindow
        {
            get;
            set;
        }

        public static Task<string> Execute(string command, string arguments = null, int timeout = 0, bool showConsole = false)
        {

            return Task.Run<string>(() =>
            {

                try
                {

                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo(command, arguments)
                        {
                            UseShellExecute = false,
                            CreateNoWindow = !showConsole || !AlwaysCreateWindow,
                            RedirectStandardOutput = true
                        }
                    };

                    if (Command != null)
                        Command(string.Format(">> {0} {1}\n\n", command, arguments));

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

                }
                catch (Exception ex)
                {

                    if (Exception != null)
                        Exception(ex);

                }

                return string.Empty;

            });

        }

        public static Task Execute(string command, string arguments, Stream outStream, Stream inStream = null, Stream errStream = null, bool showConsole = false)
        {

            if (outStream == null)
                throw new ArgumentNullException("outStream");

            return Task.Run(() =>
            {

                try
                {

                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo(command, arguments)
                        {
                            UseShellExecute = false,
                            CreateNoWindow = !showConsole || !AlwaysCreateWindow,
                            RedirectStandardOutput = true
                        }
                    };

                    if (Command != null)
                        Command(string.Format(">> {0} {1}\n\n", command, arguments));

                    if (inStream != null)
                        inStream.CopyToAsync(process.StandardInput.BaseStream);

                    if (errStream != null)
                        process.StandardError.BaseStream.CopyToAsync(errStream);

                    process.Start();

                    var baseStream = process.StandardOutput.BaseStream;
                    int lastRead = 0;

                    byte[] buffer = new byte[4096];
                    do
                    {
                        lastRead = baseStream.Read(buffer, 0, buffer.Length);
                        outStream.Write(buffer, 0, lastRead);

                    } while (lastRead > 0);

                    process.WaitForExit(10000);

                    if (Output != null)
                    {
                        Output(string.Format("[Binary length={0}]", outStream.Length));
                    }

                }
                catch (Exception ex)
                {

                    if (Exception != null)
                        Exception(ex);

                }

            });

        }

    }
}
