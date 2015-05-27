using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public static class Adb
    {

        const string ADB_PATH = "adb";
        const string ADB_SHELL_COMMAND = "shell {0}";
        const string ADB_KILLSERVER_COMMAND = "kill-server";
        const string ADB_STARTSERVER_COMMAND = "start-server";

        const string ADB_LIST_DEVICES_COMMAND = "devices";
        const string LIST_PACKAGES_COMMAND = "pm -l";
        const string INSTALL_PACKAGE_COMMAND = "install \"{0}\"";
        const string UNINSTALL_PACKAGE_COMMAND = "pm uninstall -k {0}";
        const string PUSH_FILE_COMMAND = "push \"{0}\" \"{1}\"";
        const string PULL_FILE_COMMAND = "pull \"{0}\" \"{1}\"";
        const string DELETE_FILE_COMMAND = "rm \"{0}\"";
        const string DELETE_DIRECTORY_COMMAND = "rmdir \"{0}\"";
        const string LOGCAT_COMMAND = "logcat";
        const string PS_COMMAND = "ps";
        const string REBOOT_COMMAND = "reboot";
        const string WAITFORDEVICE_COMMAND = "wait-for-device";
        
        const string DUMPSYS_COMMAND = "dumpsys";
        const string KILL_COMMAND = "kill -{0} {1}";

        const string SCREENCAP_BIN_COMMAND = "screencap -p";
        const string SCREENCAP_TOFILE_COMMAND = "screencap -p {0}";
        const string SCREENCAP_PATH_FORMAT = "\"/sdcard/{0}\"";

        const string GET_INPUT_DEVICES_COMMAND = "getevent";
        const string SEND_TEXT_INPUT_COMMAND = "input text \"{0}\"";
        const string SEND_KEYEVENT_COMMAND = "input keyevent {0}";
        const string SEND_TOUCHEVENT_COMMAND = "sendevent {0} {1} {2} {3}";
        const string START_ACTIVITY_COMMAND = "am start -n {0}/{1}";
        const string LIST_MOUNT_COMMAND = "mount";
        const string LIST_DIRECTORY_COMMAND = "ls -l {0}";
        const string SELECT_DEVICE_ARGUMENT = "-s {0} ";
        const string GET_DEVICE_PROPS_COMMAND = "getprop {0}";
        const string SET_DEVICE_PROP_COMMAND = "setprop {0} {1}";

        const string SEND_TAP_EVENT_COMMAND = "input touchscreen tap {0} {1}";
        const string SEND_SWIPE_EVENT_COMMAND = "input touchscreen swipe {0} {1} {2} {3} {4}";
        const string BEGIN_TOUCHEVENT_COMMAND = "sendevent {0} 3 57 {1}; sendevent {0} 3 53 {2}; sendevent {0} 3 54 {3}; sendevent {0} 3 48 {4}; sendevent {0} 3 58 {5}; sendevent {0} 0 2 0; sendevent {0} 0 0 0;";
        const string RELEASE_TOUCHEVENT_COMMAND = "sendevent {0} 0 2 0; sendevent {0} 0 0 0;";
        const string RELEASE_ALL_TOUCHEVENT_COMMAND = "sendevent {0} 3 57 -1; sendevent {0} 0 2 0; sendevent {0} 0 0 0;";
        

        

        static string s_AdbPath;
        static bool s_Started = false;
        static List<Device> s_Devices = new List<Device>();

        public static string AdbPath
        {
            get
            {
                return s_AdbPath ?? ADB_PATH;
            }

            set
            {
                s_AdbPath = value;
            }

        }

        public static async Task<string[]> GetAttachedDeviceNames()
        {

            var devicesOutput = await Shell.Execute(AdbPath, ADB_LIST_DEVICES_COMMAND, 1000);

            var deviceNames = new List<string>();

            var count = 0;

            foreach (var line in TokenizeString(devicesOutput))
            {

                if (count++ == 0)
                    continue;

                var tokens = TokenizeString(line, "\t");

                if (tokens.Length == 2 && tokens[0].Length > 0 && tokens[1].Contains("device"))
                {

                    deviceNames.Add(tokens[0]);

                }

            }

            return deviceNames.ToArray();

        }

        public static async Task<List<Device>> GetAttachedDevices()
        {

            var devicesOutput = await Shell.Execute(AdbPath, ADB_LIST_DEVICES_COMMAND, 1000);

            var devices = new List<Device>();

            var count = 0;

            foreach (var line in TokenizeString(devicesOutput))
            {

                if (count++ == 0)
                    continue;

                var tokens = TokenizeString(line, "\t");

                if (tokens.Length == 2 && tokens[0].Length > 0 && tokens[1].Contains("device"))
                {

                    var name = tokens[0];

                    var isEmulator = !tokens[1].Contains("device");

                    devices.Add(new Device(name, isEmulator));

                }

            }

            return devices;

        }

        public static async Task<List<string>> GetRemoteStorageLocations(Device device = null)
        {

            var mountOutput = await ExecuteAdbShellCommand(LIST_MOUNT_COMMAND, device);

            var sdMounts = TokenizeString( mountOutput ).Where( line => line.StartsWith( "/dev/block/vold/" ) && line.Contains( "/mnt/sd" ) ).ToList();

            var sdCards = new List<string>();

            foreach (var sdMount in sdMounts)
            {

                var index = sdMount.IndexOf("/mnt/sd");

                var sdCard = string.Empty;

                while (sdMount[index] != ' ')
                {
                    sdCard += sdMount[index];
                    ++index;
                }

                sdCards.Add(sdCard);

            }

            sdCards.Add("/sdcard");

            return sdCards;

        }

        public static async Task<bool> PushFile(string localFilePath, string remoteFilePath, Device device = null)
        {

            var pushCommand = string.Format(PUSH_FILE_COMMAND, localFilePath, remoteFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(pushCommand, device));

            return await RemoteFileExists(remoteFilePath, device);

        }

        public static async Task<bool> PullFile(string localFilePath, string remoteFilePath, Device device = null)
        {

            var pullCommand = string.Format(PULL_FILE_COMMAND, remoteFilePath, localFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(pullCommand, device));

            return File.Exists(localFilePath);

        }

        public static async Task<bool> RemoteFileExists(string remoteFilePath, Device device = null)
        {

            var remoteFileName = PathNix.GetFilename(remoteFilePath);

            var remoteDirectoryName = PathNix.GetDirectoryPath(remoteFilePath);

            var listing = await ListRemoteDirectory(remoteDirectoryName, device);

            return listing != null && listing.Files.Any(entry => entry.Name == remoteFileName);

        }

        public static async Task<bool> RemoteDirectoryExists(string remotePath, Device device = null)
        {

            var listing = await ListRemoteDirectory(remotePath, device);

            return listing != null && listing.Directories.Any(entry => entry.Path == remotePath);

        }

        public static async Task<DirectoryListing> ListRemoteDirectory(string remotePath, Device device = null)
        {

            var listDirectoryCommand = string.Format(LIST_DIRECTORY_COMMAND, remotePath);

            var output = await ExecuteAdbShellCommand(listDirectoryCommand, device);

            var lines = TokenizeString(output);

            var listing = new DirectoryListing()
            {
                Path = remotePath
            };

            foreach (var line in lines)
	        {
                listing.AddEntry(line.Trim());
	        }

            return listing;

        }

        public static async Task<bool> DeleteRemoteFile(string remoteFilePath, Device device = null)
        {

            var deleteFileCommand = string.Format(DELETE_FILE_COMMAND, remoteFilePath);

            var filename = PathNix.GetFilename(remoteFilePath);

            var remotePath = PathNix.GetDirectoryPath(remoteFilePath);

            var output = await ExecuteAdbShellCommand(deleteFileCommand, device);

            var listing = await ListRemoteDirectory(remotePath, device);

            return !listing.Directories.Any(entry => entry.Path == remoteFilePath);

        }

        public static async Task<bool> DeleteRemoteDirectory(string remotePath, Device device = null)
        {

            var deleteDirectoryCommand = string.Format(DELETE_DIRECTORY_COMMAND, remotePath);

            var output = await ExecuteAdbShellCommand(deleteDirectoryCommand, device);

            var listing = await ListRemoteDirectory(remotePath, device);

            return !listing.Directories.Any(entry => entry.Path == remotePath);

        }

        public static async Task<bool> IsPackageInstalled(string packageName, Device device = null)
        {

            var output = await ExecuteAdbShellCommand(LIST_PACKAGES_COMMAND, device);

            return TokenizeString(output).Any( (pkg) => {

                return pkg.StartsWith("package:" + packageName);
                
            } );

        }

        public static async Task<bool> UninstallPackage(string packageName, Device device = null)
        {

            var uninstallPackageCommand = string.Format(UNINSTALL_PACKAGE_COMMAND, packageName);

            var output = await ExecuteAdbShellCommand(uninstallPackageCommand, device);

            return TokenizeString(output).Any((pkg) =>
            {
                return pkg.StartsWith("Success");
            });

        }

        public static async Task<bool> IsPackageRunning(string packageName, Device device = null)
        {

            var processes = await GetRunningProcesses(device);

            return processes.Any(ps => ps.Name == packageName);

        }

        public static Task<bool> InstallAPK(string apkFilePath)
        {
            return InstallAPK(null, apkFilePath);
        }

        public static async Task<bool> InstallAPK(Device device, string apkFilePath )
        {

            var installCommand = string.Format(INSTALL_PACKAGE_COMMAND, apkFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection( installCommand, device ) );

            return output.ToLower().Contains( "success" );

        }

        public static async Task<bool> StartActivity(string packageName, string activity, Device device = null)
        {

            var startActivityCommand = string.Format(START_ACTIVITY_COMMAND, packageName, activity);

            var output = await ExecuteAdbShellCommand(startActivityCommand, device);

            await Task.Delay(1000);

            return await IsPackageRunning(packageName, device);

        }

        public static async Task<List<AndroidProcess>> GetRunningProcesses(Device device = null)
        {

            var output = await ExecuteAdbShellCommand(PS_COMMAND, device, 10000);

            var lines = TokenizeString(output);

            var processes = new List<AndroidProcess>();

            foreach (var line in lines.Skip(1))
            {

                if (string.IsNullOrEmpty(line))
                    continue;

                var paramz = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                processes.Add(new AndroidProcess()
                {
                    User = paramz[0],
                    Pid = int.Parse( paramz[1] ),
                    PPid = int.Parse( paramz[2] ),
                    VSize = long.Parse( paramz[3] ),
                    Rss = int.Parse( paramz[4] ),
                    Wchan = paramz[5],
                    PC = paramz[6],
                    Name = paramz[8]
                });

            }

            return processes.Skip(1).ToList();

        }

        public static Task<bool> KillProcess(int pid, Device device = null)
        {
            return SendSignalToProcess(pid, 9, device);
        }

        public static Task<bool> KillProcess(AndroidProcess process, Device device = null)
        {
            return SendSignalToProcess(process.Pid, 9, device);
        }

        public static Task<bool> SendSignalToProcess(AndroidProcess process, int signal)
        {
            return SendSignalToProcess(process, signal);
        }

        public static Task<bool> SendSignalToProcess(AndroidProcess process, int signal, Device device = null)
        {
            return SendSignalToProcess(process.Pid, signal, device);
        }

        public static async Task<bool> SendSignalToProcess(int pid, int signal, Device device = null)
        {

            var sendSignalCommand = string.Format(KILL_COMMAND, signal.ToString(), pid.ToString());

            var output = await ExecuteAdbShellCommand(ArgumentsWithDeviceSelection(sendSignalCommand, device), device, 0);

            return string.IsNullOrEmpty(output);

        }

        public static async Task<Stream> TakeScreenshot(Device device = null)
        {

            var outStream = new MemoryStream();

            await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(SCREENCAP_BIN_COMMAND, device), outStream);

            return outStream;

        }

        public static async Task<Stream> TakeScreenshot(string filePath = null, Device device = null)
        {

            if (string.IsNullOrEmpty(filePath))
                filePath = Path.GetTempFileName();

            var filename = Path.GetFileName( filePath );

            var remoteFilePath = string.Format( SCREENCAP_PATH_FORMAT, filename );

            var screencapCommand = string.Format( SCREENCAP_TOFILE_COMMAND, remoteFilePath );

            var output = await ExecuteAdbShellCommand(screencapCommand, device);

            var pulled = await PullFile(filePath, remoteFilePath);

            if (pulled && File.Exists( filePath ) )
            {

                await DeleteRemoteFile(remoteFilePath, device);

                return File.OpenRead( filePath );

            }
            else
                return null;

        }

        public static async Task<List<InputDevice>> GetInputDevices(Device device = null)
        {

            var devices = new List<InputDevice>();

            var output = await ExecuteAdbShellCommand( GET_INPUT_DEVICES_COMMAND, device, 1000);

            var lines = TokenizeString(output);

            var enumerator = lines.GetEnumerator();

            while (enumerator.MoveNext())
            {

                var line = enumerator.Current as string;

                if (line.ToLower().StartsWith("add device"))
                {

                    enumerator.MoveNext();

                    var line2 = enumerator.Current as string;

                    var path = line.Split(new char[] { ':' }, StringSplitOptions.None)[1].Trim();

                    var name = line2.Split(new char[] { ':' }, StringSplitOptions.None)[1].Trim().Replace("\"",string.Empty);

                    devices.Add( new InputDevice(){
                        Name = name,
                        Path = path
                    });

                }

            }

            return devices;

        }

        public static Task SendTextInput(string text, Device device = null)
        {

            var sendTextInputCommand = string.Format( SEND_TEXT_INPUT_COMMAND, text.Replace(" ", "%s") );

            return ExecuteAdbShellCommand(sendTextInputCommand, device);

        }

        public static Task SendKeyEvent(Keycode key, Device device = null)
        {
            return SendKeyEvent((int)key, device);
        }

        public static Task SendKeyEvent(int key, Device device = null)
        {

            var keyEventCommand = string.Format(SEND_KEYEVENT_COMMAND, key.ToString());

            return ExecuteAdbShellCommand(keyEventCommand, device);

        }

        public static Task SendAbsEvent(InputDevice inputDevice, AbsEventParam absParam, int value, Device device = null)
        {

            var sendAbsEventCommand = string.Format(SEND_TOUCHEVENT_COMMAND, inputDevice.Path, (int)EventType.Abs, (int)absParam, value);

            return ExecuteAdbShellCommand(sendAbsEventCommand, device);

        }

        public static Task SendSynEvent(InputDevice inputDevice, SynEventParam synParam, int value, Device device = null)
        {

            var sendAbsEventCommand = string.Format(SEND_TOUCHEVENT_COMMAND, inputDevice.Path, (int)EventType.Abs, (int)synParam, value);

            return ExecuteAdbShellCommand(sendAbsEventCommand, device);

        }

        public static Task SendTouchEvent(InputDevice inputDevice, int x, int y, int fingerIndex = 0, int fingerWidth = 5, int pressure = 50, Device device = null)
        {

            var sendTouchEventCommand = string.Format(BEGIN_TOUCHEVENT_COMMAND, inputDevice.Path, fingerIndex, x, y, fingerWidth, pressure);

            return ExecuteAdbShellCommand(sendTouchEventCommand, device);

        }

        public static Task SendReleaseTouchEvent(InputDevice inputDevice, int fingerIndex = 0, Device device = null)
        {

            var sendReleaseTouchEventCommand = string.Format(RELEASE_ALL_TOUCHEVENT_COMMAND, inputDevice.Path);

            return ExecuteAdbShellCommand(sendReleaseTouchEventCommand, device);

        }

        public static Task SendTapEvent(int x, int y, Device device = null)
        {

            var sendTapEventCommand = string.Format( SEND_TAP_EVENT_COMMAND, x, y );

            return ExecuteAdbShellCommand(sendTapEventCommand, device);

        }

        public static async Task<PropTree> GetDevicePropTree(Device device = null)
        {

            var getPropCommand = string.Format(GET_DEVICE_PROPS_COMMAND, string.Empty);

            var output = await ExecuteAdbShellCommand(getPropCommand, device);

            var tokenizedProps = TokenizeString(output);

            var tree = new PropTree();

            foreach (var propLine in tokenizedProps)
            {

                if (string.IsNullOrEmpty(propLine))
                    continue;

                var propTokens = propLine.Split(new string[] { "]: [" }, StringSplitOptions.RemoveEmptyEntries);

                var key = propTokens[0].Substring(1);

                var value = propTokens[1].Substring(0, propTokens[1].Length - 3);

                tree.AddProp(key, value);

            }

            return tree;

        }

        public static async Task<Prop> GetDeviceProp(string propKey, Device device = null)
        {

            var getPropCommand = string.Format(GET_DEVICE_PROPS_COMMAND, propKey);

            var propValue = await ExecuteAdbShellCommand(getPropCommand, device);

            return new Prop()
            {
                Key = propKey,
                Value = propValue.Trim(),
                Name = propKey.Split( new char[]{'.'} ).Last()
            };

        }

        public static Task<string> SetDeviceProp(string propKey, string value)
        {
            return SetDeviceProp(null, propKey, value);
        }

        public static Task<string> SetDeviceProp(Device device, string propKey, string value)
        {

            var setPropCommand = string.Format(SET_DEVICE_PROP_COMMAND, propKey);

            return ExecuteAdbCommand(setPropCommand, device);

        }

        public static Task<string> GetLogcatOutput( Device device = null )
        {
            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(LOGCAT_COMMAND, device), 5000);
        }

        public static Task<string> GetDumpsysOutput(Device device = null)
        {
            return ExecuteAdbShellCommand( DUMPSYS_COMMAND, device, 0);
        }

        public static Task Reboot( bool waitForBoot = false, Device device = null)
        {

            var rebootTask = ExecuteAdbCommand(REBOOT_COMMAND, device);

            if (waitForBoot)
            {

                rebootTask.Wait();

                return ExecuteAdbCommand(WAITFORDEVICE_COMMAND, device);

            }
            else
                return rebootTask;

        }

        public static Task<string> ExecuteAdbCommand(string command, int timeout = 0)
        {

            EnsureServerIsRunning();

            return Shell.Execute(AdbPath, command, timeout);

        }

        public static Task<string> ExecuteAdbCommand(string command, Device device = null, int timeout = 0)
        {

            EnsureServerIsRunning();

            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(command, device), timeout);
        }

        public static Task<string> ExecuteAdbShellCommand(string command, int timeout = 0)
        {

            EnsureServerIsRunning();

            var adbShellCommand = string.Format(ADB_SHELL_COMMAND, command);

            return Shell.Execute(AdbPath, adbShellCommand, timeout);

        }

        public static Task<string> ExecuteAdbShellCommand(string command, Device device = null, int timeout = 0)
        {

            EnsureServerIsRunning();

            var adbShellCommand = string.Format(ADB_SHELL_COMMAND, command);

            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(adbShellCommand, device), timeout);

        }

        static string ArgumentsWithDeviceSelection(string command, Device device = null)
        {
            return ArgumentsWithDeviceSelection( device != null ? device.Name : string.Empty, command);
        }

        static string ArgumentsWithDeviceSelection(string deviceName, string command)
        {

            if (string.IsNullOrEmpty(deviceName))
                return command;

            return string.Format("-s {0} {1}", deviceName, command);
        }

        static async void EnsureServerIsRunning()
        {
            if (!s_Started)
                await Shell.Execute(AdbPath, ADB_STARTSERVER_COMMAND);
        }

        static string[] TokenizeString(string text, string separator = "\n")
        {

            if (text == null)
                return new string[0];

            return text.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);

        }

    }

}
