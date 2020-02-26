// <copyright file="LCTFController.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;

namespace ChemImage.LCTF
{
	/// <summary>
	/// Handler for when an MCF attaches to the computer.
	/// </summary>
	public delegate void OnMcfAttachedHandler();

	/// <summary>
	/// Handler for when an MCF detaches from the computer.
	/// </summary>
	public delegate void OnMcfDetachedHandler();

	/// <summary>
	/// Singleton class which handles detecting and connecting to MCFs.
	/// </summary>
#pragma warning disable CA1063 // Implement IDisposable Correctly
	public class LCTFController : IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
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
			this.usbFinder = new UsbDeviceFinder(new Guid("{d67436ae-96c7-4da3-83c9-322c4ceb41f3}"));
			this.usbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
			this.usbDeviceNotifier.OnDeviceNotify += this.OnDeviceNotify;
			this.usbDeviceNotifier.Enabled = true;

			this.UpdateAttachedDevices();
		}

		/// <summary>
		/// Event for when an MCF is attached to the computer.
		/// </summary>
		public event OnMcfAttachedHandler OnMcfAttached;

		/// <summary>
		/// Event for when an MCF is detached from the computer.
		/// </summary>
		public event OnMcfDetachedHandler OnMcfDetached;

		/// <summary>
		/// Gets a singleton instance of <see cref="LCTFController"/>
		/// This instance must be referenced first from the main thread.
		/// If not, LibUSB can't set up correctly and events from Windows won't work. Dispose this instance when you're done with it.
		/// </summary>
		public static LCTFController Instance
		{
			get
			{
				return PrivateInstance;
			}
		}

		/// <summary>
		/// Gets all of the currently attached LCTFs.
		/// </summary>
		public IEnumerable<LCTFDevice> AttachedLCTFs
		{
			get
			{
				return this.LCTFs.Values;
			}
		}

		private Dictionary<UsbRegistry, LCTFDevice> LCTFs { get; } = new Dictionary<UsbRegistry, LCTFDevice>();

		/// <summary>
		/// Disposes the McfController and any McfDevice objects.
		/// </summary>
		public void Dispose()
		{
			foreach (var mcf in this.AttachedLCTFs)
			{
				mcf.Dispose();
			}

			this.usbDeviceNotifier.Enabled = false;
			this.usbDeviceNotifier.OnDeviceNotify -= this.OnDeviceNotify;
			UsbDevice.Exit();
		}

		/// <summary>
		/// Gets the first MCF from the AttachedMcfs IEnumerable.
		/// </summary>
		/// <returns>Null if no MCFs are attached. Otherwise the first MCF from the AttachedMcfs IEnumerable.</returns>
		public LCTFDevice GetFirstLCTF()
		{
			return this.AttachedLCTFs.FirstOrDefault();
		}

		private void OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
		{
			this.UpdateAttachedDevices();
		}

		private void UpdateAttachedDevices()
		{
			var newRegistryEntries = UsbDevice.AllDevices.Where((device) => this.usbFinder.Check(device));

			// Add new MCFs
			foreach (var newRegistryEntry in newRegistryEntries)
			{
				var match = this.LCTFs.Keys.Where((x) => x.SymbolicName.Equals(newRegistryEntry.SymbolicName, StringComparison.InvariantCulture)).FirstOrDefault();
				if (match == null)
				{
					var device = UsbDevice.OpenUsbDevice(x => x.DevicePath == newRegistryEntry.DevicePath);
					if (device != null)
					{
						this.LCTFs.Add(newRegistryEntry, new LCTFDevice(device));
						this.OnMcfAttached?.Invoke();
					}
				}
			}

			// Remove lost MCFs
			var tempMcfs = this.LCTFs.Keys.ToList();

			foreach (var registryEntry in tempMcfs)
			{
				var match = newRegistryEntries.Where((x) => x.SymbolicName.Equals(registryEntry.SymbolicName, StringComparison.InvariantCulture)).FirstOrDefault();
				if (match == null)
				{
					this.LCTFs[registryEntry].Dispose();
					this.LCTFs.Remove(registryEntry);
					this.OnMcfDetached?.Invoke();
				}
			}
		}
	}
}
