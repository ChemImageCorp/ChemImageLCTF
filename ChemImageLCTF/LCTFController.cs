using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	public class McfController : IDisposable
	{
		#region Private Variables

		private static readonly McfController instance = new McfController();

		private IDeviceNotifier _usbDeviceNotifier;
		private UsbDeviceFinder _usbFinder;

		private Dictionary<UsbRegistry, LCTFDevice> _mcfs { get; } = new Dictionary<UsbRegistry, LCTFDevice>();

		#endregion

		#region Constructors

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static McfController()
		{

		}

		private McfController()
		{
			_usbFinder = new UsbDeviceFinder(new Guid("{d67436ae-96c7-4da3-83c9-322c4ceb41f3}"));
			_usbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
			_usbDeviceNotifier.OnDeviceNotify += OnDeviceNotify;
			_usbDeviceNotifier.Enabled = true;

			UpdateAttachedDevices();
		}


		#endregion

		#region Properties
		/// <summary>
		/// Event for when an MCF is attached to the computer.
		/// </summary>
		public event OnMcfAttachedHandler OnMcfAttached;

		/// <summary>
		/// Event for when an MCF is detached from the computer.
		/// </summary>
		public event OnMcfDetachedHandler OnMcfDetached;

		/// <summary>
		/// All of the currently attached MCFs.
		/// </summary>
		public IEnumerable<LCTFDevice> AttachedMcfs
		{
			get
			{
				return _mcfs.Values;
			}
		}

		/// <summary>
		/// This instance must be referenced first from the main thread. If not, LibUSB can't set up correctly and events from Windows won't work. Dispose this instance when you're done with it.
		/// </summary>
		public static McfController Instance
		{
			get
			{
				return instance;
			}
		}
		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the first MCF from the AttachedMcfs IEnumerable.
		/// </summary>
		/// <returns>Null if no MCFs are attached. Otherwise the first MCF from the AttachedMcfs IEnumerable.</returns>
		public LCTFDevice GetFirstMCF()
		{
			return AttachedMcfs.FirstOrDefault();
		}

		/// <summary>
		/// Disposes the McfController and any McfDevice objects.
		/// </summary>
		public void Dispose()
		{
			foreach (var mcf in AttachedMcfs)
			{
				mcf.Dispose();
			}

			_usbDeviceNotifier.Enabled = false;
			_usbDeviceNotifier.OnDeviceNotify -= OnDeviceNotify;
			UsbDevice.Exit();
		}

		#endregion

		#region Private Methods

		private void OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
		{
			UpdateAttachedDevices();
		}

		private void UpdateAttachedDevices()
		{
			var newRegistryEntries = UsbDevice.AllDevices.Where((device) => _usbFinder.Check(device));

			// Add new MCFs
			foreach (var newRegistryEntry in newRegistryEntries)
			{
				var match = _mcfs.Keys.Where((x) => x.SymbolicName.Equals(newRegistryEntry.SymbolicName)).FirstOrDefault();
				if (match == null)
				{
					var device = UsbDevice.OpenUsbDevice(x => x.DevicePath == newRegistryEntry.DevicePath);
					if (device != null)
					{
						_mcfs.Add(newRegistryEntry, new LCTFDevice(device));
						OnMcfAttached?.Invoke();
					}
				}
			}

			// Remove lost MCFs
			var tempMcfs = _mcfs.Keys.ToList();

			foreach (var registryEntry in tempMcfs)
			{
				var match = newRegistryEntries.Where((x) => x.SymbolicName.Equals(registryEntry.SymbolicName)).FirstOrDefault();
				if (match == null)
				{
					_mcfs[registryEntry].Dispose();
					_mcfs.Remove(registryEntry);
					OnMcfDetached?.Invoke();
				}
			}
		}

		#endregion
	}
}
