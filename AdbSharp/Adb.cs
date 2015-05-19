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

        const string ADB_LIST_DEVICES_COMMAND = "devices";
        const string LIST_PACKAGES_COMMAND = "shell pm -l";
        const string INSTALL_PACKAGE_COMMAND = "install \"{0}\"";
        const string UNINSTALL_PACKAGE_COMMAND = "shell pm uninstall -k {0}";
        const string PUSH_FILE_COMMAND = "push \"{0}\" \"{1}\"";
        const string PULL_FILE_COMMAND = "pull \"{0}\" \"{1}\"";
        const string LOGCAT_COMMAND = "logcat";
        const string PS_COMMAND = "shell ps";
        const string ADB_SHELL_COMMAND = "shell {0}";
        const string DUMPSYS_COMMAND = "shell dumpsys";
        const string KILL_COMMAND = "shell kill -{0} {1}";


        const string SEND_KEYEVENT_COMMAND = "shell input keyevent {0}";
        const string START_ACTIVITY_COMMAND = "shell am start -n {0}/{1}";
        const string LIST_MOUNT_COMMAND = "shell mount";
        const string LIST_DIRECTORY_COMMAND = "shell ls {0}";
        const string SELECT_DEVICE_ARGUMENT = "-s {0} ";
        const string GET_DEVICE_PROPS_COMMAND = "shell getprop {0}";
        const string SET_DEVICE_PROP_COMMAND = "shell setprop {0} {1}";

        const string ADB_PATH = "adb";

        static string s_AdbPath;

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

                if (tokens.Length == 2 && tokens[0].Length > 0)
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

                if (tokens.Length == 2 && tokens[0].Length > 0)
                {

                    var deviceName = tokens[0];

                    var isEmulator = !tokens[1].Contains("device");

                    devices.Add( new Device(deviceName, isEmulator) );

                }

            }

            return devices;

        }

        public static async Task<List<string>> GetRemoteStorageLocations()
        {
            return await GetRemoteStorageLocations(null);
        }

        public static async Task<List<string>> GetRemoteStorageLocations(Device device)
        {

            var mountOutput = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, LIST_MOUNT_COMMAND));

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

        public static async Task<bool> PushFile(string localFilePath, string remoteFilePath)
        {
            return await PushFile(null, localFilePath, remoteFilePath);
        }

        public static async Task<bool> PushFile(Device device, string localFilePath, string remoteFilePath)
        {

            var pushCommand = string.Format(PUSH_FILE_COMMAND, localFilePath, remoteFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, pushCommand));

            return await RemoteFileExists( device, remoteFilePath );

        }

        public static async Task<bool> PullFile(string localFilePath, string remoteFilePath)
        {
            return await PullFile(null, localFilePath, remoteFilePath);
        }

        public static async Task<bool> PullFile(Device device, string localFilePath, string remoteFilePath)
        {

            var pushCommand = string.Format(PULL_FILE_COMMAND, localFilePath, remoteFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, pushCommand));

            return await RemoteFileExists(device, remoteFilePath);

        }

        public static async Task<bool> RemoteFileExists(string remoteFilePath)
        {
            return await RemoteFileExists(null, remoteFilePath);
        }

        public static async Task<bool> RemoteFileExists(Device device, string remoteFilePath)
        {

            var remoteFileName = Path.GetFileName( remoteFilePath );

            var remoteDirectoryName = remoteFilePath.Replace(remoteFileName, string.Empty);

            var output = await ListRemoteDirectory( device, remoteDirectoryName );

            return output.Any(line => line.Substring(0, line.Length - 2 ) == remoteFileName);

        }

        public static async Task<List<string>> ListRemoteDirectory(string remotePath)
        {
            return await ListRemoteDirectory(null, remotePath);
        }

        public static async Task<List<string>> ListRemoteDirectory(Device device, string remotePath)
        {

            var listDirectoryCommand = string.Format(LIST_DIRECTORY_COMMAND, remotePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, listDirectoryCommand));

            return TokenizeString(output).ToList();

        }

        public static async Task<bool> IsPackageInstalled(string packageName)
        {
            return await IsPackageInstalled(null, packageName);
        }

        public static async Task<bool> IsPackageInstalled( Device device, string packageName)
        {

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, LIST_PACKAGES_COMMAND));

            return TokenizeString(output).Any( (pkg) => {

                return pkg.StartsWith("package:" + packageName);
                
            } );

        }

        public static async Task<bool> UninstallPackage(string packageName)
        {
            return await UninstallPackage(null, packageName);
        }

        public static async Task<bool> UninstallPackage(Device device, string packageName)
        {

            var uninstallPackageCommand = string.Format(UNINSTALL_PACKAGE_COMMAND, packageName);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, uninstallPackageCommand));

            return TokenizeString(output).Any((pkg) =>
            {
                return pkg.StartsWith("Success");
            });

        }

        public static async Task<bool> IsPackageRunning(string packageName)
        {
            return await IsPackageRunning(null, packageName);
        }

        public static async Task<bool> IsPackageRunning(Device device, string packageName)
        {

            var processes = await GetRunningProcesses(device);

            return processes.Any(ps => ps.Name == packageName);

        }

        public static async Task<bool> InstallAPK(string apkFilePath)
        {
            return await InstallAPK(null, apkFilePath);
        }

        public static async Task<bool> InstallAPK(Device device, string apkFilePath )
        {

            var installCommand = string.Format(INSTALL_PACKAGE_COMMAND, apkFilePath);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, installCommand));

            return output.ToLower().Contains( "success" );

        }

        public static async Task<bool> StartActivity(string packageName, string activity)
        {
            return await StartActivity(null, packageName, activity);
        }

        public static async Task<bool> StartActivity(Device device, string packageName, string activity)
        {

            var startActivityCommand = string.Format(START_ACTIVITY_COMMAND, packageName, activity);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, startActivityCommand));

            return await IsPackageRunning( device, packageName );

        }

        public static async Task<List<AndroidProcess>> GetRunningProcesses(Device device = null)
        {

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, PS_COMMAND));

            var lines = TokenizeString(output);

            var processes = new List<AndroidProcess>();

            foreach (var line in lines)
            {

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

        public static Task<bool> SendSignalToProcess(int pid, int signal)
        {
            return SendSignalToProcess(pid, signal);
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

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, sendSignalCommand), 0);

            return string.IsNullOrEmpty(output);

        }

        public static Task SendKeyEvent(Keycode key, Device device = null)
        {
            return SendKeyEvent((int)key, device);
        }

        public static Task SendKeyEvent(int key, Device device = null)
        {

            var keyEventCommand = string.Format(SEND_KEYEVENT_COMMAND, key.ToString());

            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, keyEventCommand));

        }

        public static Task<Prop> GetDeviceProp(string propKey)
        {
            return GetDeviceProp(null, propKey);
        }

        public static async Task<Prop> GetDeviceProp(Device device, string propKey)
        {

            var propTree = await GetDevicePropTree(device);

            return propTree.FindProp(propKey);

        }

        public static async Task<PropTree> GetDevicePropTree(Device device = null)
        {

            var getPropCommand = string.Format(GET_DEVICE_PROPS_COMMAND, string.Empty);

            var output = await Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, getPropCommand));

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

        public static Task<string> SetDeviceProp(string propKey, string value)
        {
            return SetDeviceProp(null, propKey, value);
        }

        public static Task<string> SetDeviceProp(Device device, string propKey, string value)
        {

            var setPropCommand = string.Format(SET_DEVICE_PROP_COMMAND, propKey);

            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, setPropCommand));

        }

        public static Task<string> GetLogcatOutput( Device device = null )
        {
            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, LOGCAT_COMMAND), 5000);
        }

        public static Task<string> GetDumpsysOutput(Device device = null)
        {
            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, DUMPSYS_COMMAND), 0);
        }

        public static Task<string> ExecuteAdbCommand(string command, int timeout = 0)
        {
            return Shell.Execute(AdbPath, command, timeout);
        }

        public static Task<string> ExecuteAdbCommand(string command, Device device = null, int timeout = 0)
        {
            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection(device, command), timeout);
        }

        public static Task<string> ExecuteAdbShellCommand(string command, int timeout = 0)
        {

            var adbShellCommand = string.Format(ADB_SHELL_COMMAND, command);

            return Shell.Execute(AdbPath, adbShellCommand, timeout);

        }

        public static Task<string> ExecuteAdbShellCommand(string command, Device device = null, int timeout = 0)
        {

            var adbShellCommand = string.Format(ADB_SHELL_COMMAND, command);

            return Shell.Execute(AdbPath, ArgumentsWithDeviceSelection( device, adbShellCommand ), timeout);

        }

        static string ArgumentsWithDeviceSelection(Device device, string command)
        {
            return ArgumentsWithDeviceSelection( device != null ? device.Name : null, command);
        }

        static string ArgumentsWithDeviceSelection(string deviceName, string command)
        {

            if (string.IsNullOrEmpty(deviceName))
                return command;

            return string.Format("-s {0} {1}", deviceName, command);
        }

        static string[] TokenizeString(string text, string separator = "\n")
        {

            if (text == null)
                return new string[0];

            return text.Split(new string[] { separator }, StringSplitOptions.None);

        }

    }

}
