// <copyright file="LCTFController.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;

namespace ChemImage.LCTF
{
	/// <summary>
	/// Handler for when an LCTF attaches to the computer.
	/// </summary>
	public delegate void OnLctfAttachedHandler();

	/// <summary>
	/// Handler for when an LCTF detaches from the computer.
	/// </summary>
	public delegate void OnLctfDetachedHandler();

	/// <summary>
	/// Singleton class which handles detecting and connecting to LCTFs.
	/// </summary>
	public partial class LCTFController
	{
		private static readonly LCTFController PrivateInstance = new LCTFController();

		private IDeviceNotifier usbDeviceNotifier;
		private UsbDeviceFinder usbFinder;

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static LCTFController()
		{
		}

		private LCTFController()
		{
			CheckForMainThread();

			this.usbFinder = new UsbDeviceFinder(new Guid("{d67436ae-96c7-4da3-83c9-322c4ceb41f3}"));
			this.usbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
			this.usbDeviceNotifier.OnDeviceNotify += this.OnDeviceNotify;
			this.usbDeviceNotifier.Enabled = true;

			this.UpdateAttachedDevices();
		}

		private static void CheckForMainThread()
		{
			if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
				!Thread.CurrentThread.IsBackground && !Thread.CurrentThread.IsThreadPoolThread && Thread.CurrentThread.IsAlive)
			{
				MethodInfo correctEntryMethod = Assembly.GetEntryAssembly().EntryPoint;
				StackTrace trace = new StackTrace();
				StackFrame[] frames = trace.GetFrames();
				for (int i = frames.Length - 1; i >= 0; i--)
				{
					MethodBase method = frames[i].GetMethod();
					if (correctEntryMethod == method)
					{
						return;
					}
				}
			}

			throw new Exception("The first reference to LCTFController must be on the main thread for USB to function correctly.");
		}

		/// <summary>
		/// Event for when an LCTF is attached to the computer.
		/// </summary>
		public static event OnLctfAttachedHandler OnLctfAttached;

		/// <summary>
		/// Event for when an LCTF is detached from the computer.
		/// </summary>
		public static event OnLctfDetachedHandler OnLctfDetached;

		/// <summary>
		/// Gets a singleton instance of <see cref="LCTFController"/>
		/// This instance must be referenced first from the main thread.
		/// If not, LibUSB can't set up correctly and events from Windows won't work. Dispose this instance when you're done with it.
		/// </summary>
		private static LCTFController Instance
		{
			get
			{
				return PrivateInstance;
			}
		}

		/// <summary>
		/// Gets all of the currently attached LCTFs.
		/// </summary>
		public static IEnumerable<LCTFDevice> AttachedLCTFs
		{
			get
			{
				return Instance.LCTFs.Values;
			}
		}

		private Dictionary<UsbRegistry, LCTFDevice> LCTFs { get; } = new Dictionary<UsbRegistry, LCTFDevice>();

		/// <summary>
		/// Gets the first LCTF from the AttachedLCTFs IEnumerable.
		/// </summary>
		/// <returns>Null if no LCTFs are attached. Otherwise the first LCTF from the AttachedMcfs IEnumerable.</returns>
		public static LCTFDevice GetFirstLCTF()
		{
			return LCTFController.AttachedLCTFs.FirstOrDefault();
		}

		private void OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
		{
			this.UpdateAttachedDevices();
		}

		private void UpdateAttachedDevices()
		{
			var newRegistryEntries = UsbDevice.AllDevices.Where((device) => this.usbFinder.Check(device));

			// Add new LCTFs
			foreach (var newRegistryEntry in newRegistryEntries)
			{
				var match = this.LCTFs.Keys.Where((x) => x.SymbolicName.Equals(newRegistryEntry.SymbolicName, StringComparison.InvariantCulture)).FirstOrDefault();
				if (match == null)
				{
#pragma warning disable CA2000 // Dispose objects before losing scope. Can't dispose here because it needs to be used later.
					var device = UsbDevice.OpenUsbDevice(x => x.DevicePath == newRegistryEntry.DevicePath);
#pragma warning restore CA2000 // Dispose objects before losing scope
					if (device != null)
					{
						this.LCTFs.Add(newRegistryEntry, new LCTFDevice(device));
						LCTFController.OnLctfAttached?.Invoke();
					}
				}
			}

			// Remove lost LCTFs
			var tempMcfs = this.LCTFs.Keys.ToList();

			foreach (var registryEntry in tempMcfs)
			{
				var match = newRegistryEntries.Where((x) => x.SymbolicName.Equals(registryEntry.SymbolicName, StringComparison.InvariantCulture)).FirstOrDefault();
				if (match == null)
				{
					this.LCTFs[registryEntry].Dispose();
					this.LCTFs.Remove(registryEntry);
					LCTFController.OnLctfDetached?.Invoke();
				}
			}
		}
	}
}
