﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{

    public class Device{

		public Device( string name, bool isEmulator = false )
        {
			Name = name;
            IsEmulator = isEmulator;
		}

		public string Name{
			get;
			private set;
		}

        public bool IsEmulator
        {
            get;
            private set;
        }

        public InputDevice DefaultInputDevice
        {
            get;
            set;
        }

		public async Task<bool> IsAttached ()
		{

			var deviceNames = await Adb.GetAttachedDeviceNames ();

            return deviceNames.Any(name => name == Name);

		}

        public Task<List<string>> GetStorageLocations()
        {
            return Adb.GetRemoteStorageLocations(this);
        }

        public Task<bool> PushFile(string localFilePath, string remoteFilePath)
        {
            return Adb.PushFile(this,localFilePath, remoteFilePath);
        }

        public Task<bool> FileExists(string filePath)
        {
            return Adb.RemoteFileExists(this, filePath);
        }

        public Task<List<string>> ListDirectory(string path)
        {
            return Adb.ListRemoteDirectory(this, path);
        }

        public Task<bool> InstallAPK(string apkFilePath)
        {
            return Adb.InstallAPK(this, apkFilePath);
        }

		public Task<bool> IsPackageInstalled( string packageName )
		{
            return Adb.IsPackageInstalled(this, packageName);
		}

        public Task<bool> UninstallPackage(string packageName)
        {
            return Adb.UninstallPackage(this, packageName);
        }

        public Task<bool> IsPackageRunning(string packageName)
        {
            return Adb.IsPackageRunning(this, packageName);
        }

        public Task<bool> StartActivity(string packageName, string activity)
        {
            return Adb.StartActivity(this, packageName, activity);
        }

        public Task<Prop> GetProp(string propKey)
        {
            return Adb.GetDeviceProp(this, propKey);
        }

        public Task<PropTree> GetPropTree()
        {
            return Adb.GetDevicePropTree(this);
        }

        public Task<List<AndroidProcess>> GetRunningProcesses()
        {
            return Adb.GetRunningProcesses(this);
        }

        public Task<bool> KillProcess(int pid)
        {
            return Adb.KillProcess(pid, this);
        }

        public Task<bool> KillProcess(AndroidProcess process)
        {
            return Adb.KillProcess(process, this);
        }

        public Task<bool> SendSignalToProcess(int pid, int signal)
        {
            return Adb.SendSignalToProcess(pid, signal, this);
        }

        public Task<bool> SendSignalToProcess(AndroidProcess process, int signal)
        {
            return Adb.SendSignalToProcess(process, signal, this);
        }

        public Task<List<InputDevice>> GetInputDevices()
        {
            return Adb.GetInputDevices(this);
        }

        public Task SendTextInput(string text)
        {
            return Adb.SendTextInput(text, this);
        }

        public Task SendKeyEvent(Keycode key)
        {
            return Adb.SendKeyEvent(key, this);
        }

        public Task SendKeyEvent(int key)
        {
            return Adb.SendKeyEvent(key, this);
        }

        public Task SendTouchEvent(int x, int y, InputDevice inputDevice = null, int fingerIndex = 0, int fingerWidth = 5, int pressure = 50)
        {
            return Adb.SendTouchEvent(inputDevice ?? DefaultInputDevice, x, y, fingerIndex, fingerWidth, pressure, this);
        }

        public Task SendReleaseTouchEvent(int fingerIndex = 0, InputDevice inputDevice = null)
        {
            return Adb.SendReleaseTouchEvent(inputDevice ?? DefaultInputDevice, fingerIndex, this);
        }

        public Task SendTapEvent( int x, int y)
        {
            return Adb.SendTapEvent(x, y);
        }

		public Task<string> GetLogcatOutput()
        {
            return Adb.GetLogcatOutput(this);
		}

        public Task<string> GetDumpsysOutput()
        {
            return Adb.GetDumpsysOutput(this);
        }

        public override string ToString()
        {
            return Name + (IsEmulator ? " (emulator)" : string.Empty);
        }

	}


}