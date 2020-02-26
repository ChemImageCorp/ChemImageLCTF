using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChemImage.LCTF
{
	public class LCTFDevice : IDisposable
	{
		#region Events
		/// <summary>
		/// Event for when the MCF is done tuning. 
		/// When overdrive is on, this fires after the overdrive delay. 
		/// With overdrive off, it fires as soon as channel voltages are set.
		/// This event occurs for both lambda tuning and setting voltages.
		/// <para>This event is called from multiple threads.</para>
		/// </summary>
		public event OnTuningDoneHandler OnTuningDone;

		/// <summary>
		/// Event for when the MCF is done calibrating.
		/// This happens after calibration when the MCF power cycles or a Calibrate command is given.
		/// <para>This event is called from multiple threads.</para>
		/// </summary>
		public event OnCalibrationDoneHandler OnCalibrationDone;

		/// <summary>
		/// Event for when the MCF changes states.
		/// <para>This event is called from multiple threads.</para>
		/// </summary>
		public event OnStateChangedHandler OnStateChanged;

		/// <summary>
		/// Event for when the filter has an error. Nothing currently fires this.
		/// </summary>
		public event OnErrorHandler OnError;

		/// <summary>
		/// Event for when the filter was busy and couldn't handle a command. Usually due to trying to
		/// tune or change voltage during Calibration or Tuning.
		/// </summary>
		public event OnBusyHandler OnBusy;
		#endregion Events

		private UsbDevice _usbDevice;
		private UsbEndpointReader _interruptReader;

		/// <summary>
		/// The underlying USB DevicePath.
		/// </summary>
		public string InstanceId { get; private set; }

		/// <summary>
		/// Static information about the device.
		/// </summary>
		public LCTFDeviceInfo DeviceInfo { get; private set; }

		public int WavelengthMin { get; private set; }
		public int WavelengthMax { get; private set; }
		public int WavelengthStep { get; private set; }

		/// <summary>
		/// Gets the current state of the LCTF.
		/// </summary>
		public LCTFState State 
		{
			get
			{
				return (LCTFState)GetByte((byte)CommandIndices.LCTFState);
			}
		}

		/// <summary>
		/// Creates an <see cref="LCTFDevice"/> from a LibUsb <see cref="UsbDevice"/> object.
		/// </summary>
		/// <param name="underlyingWinUsbDevice">The <see cref="UsbDevice"/> for the LCTF.</param>
		public LCTFDevice(UsbDevice underlyingWinUsbDevice)
		{
			_usbDevice = underlyingWinUsbDevice;

			InstanceId = underlyingWinUsbDevice.DevicePath;

			DeviceInfo = GetDeviceInfo();

			this.WavelengthMin = (int)(GetFloat((byte)CommandIndices.WavelengthMin) + 0.5f);
			this.WavelengthMax = (int)(GetFloat((byte)CommandIndices.WavelengthMax) + 0.5f);
			this.WavelengthStep = (int)(GetFloat((byte)CommandIndices.WavelengthStep) + 0.5f);

			UsbDevice.UsbErrorEvent += UsbDevice_UsbErrorEvent;

			// Set up reader for interrupts
			_interruptReader = _usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
			_interruptReader.DataReceivedEnabled = true;
			_interruptReader.DataReceived += Reader_DataReceived;
		}

		/// <summary>
		/// Gets information about the device. Includes serial number and firmware version.
		/// </summary>
		public LCTFDeviceInfo GetDeviceInfo()
		{
			LCTFDeviceInfo deviceInfo = new LCTFDeviceInfo();

			deviceInfo.SerialNumber = GetSerial();

			var bcdVersion = (ushort)_usbDevice.Info.Descriptor.BcdDevice;
			deviceInfo.FirmwareVersion = (ushort)((bcdVersion >> 8 & 0x00ff) * 100 + (bcdVersion & 0x00ff));

			return deviceInfo;
		}

		/// <summary>
		/// Gets the current internal temperature of the LCTF.
		/// </summary>
		/// <returns>The temperature in degrees C</returns>
		public float GetTemperature()
		{
			var temp = GetFloat((byte)CommandIndices.GetTemperature);
			return temp;
		}

		/// <summary>
		/// Tunes the LCTF to a specified wavelength and returns immediately.
		/// The LCTF will not be at the specified wavelength until <see cref="OnTuningDone"/> fires.
		/// </summary>
		/// <param name="wavelength">The wavelength to tune to.</param>
		public void SetWavelength(int wavelength)
		{
			if (wavelength < this.WavelengthMin || wavelength > this.WavelengthMax)
			{
				throw new ArgumentException("Requested wavelength is outside the limits of the limit.", nameof(wavelength));
			}

			SetParameter((byte)CommandIndices.SetWavelength, wavelength);
		}

		/// <summary>
		/// Sets the LCTF to a specified wavelength and returns after <see cref="OnTuningDone"/> fires.
		/// </summary>
		/// <param name="wavelength">The wavelength to tune to.</param>
		/// <returns>The wavelength that was set.</returns>
		public async Task<int> SetWavelengthAsync(int wavelength)
		{
			Task<int> tuneTask = null;

			tuneTask = this.WaitForTune(1000);
			this.SetWavelength(wavelength);

			return await tuneTask;
		}

		/// <summary>
		/// Returns the next time a tuning done event fires.
		/// </summary>
		/// <param name="timeout">The number of milliseconds to wait before timing out.</param>
		/// <returns>The wavelength/lambda returned from the tuning done event.</returns>
		/// <exception cref="TimeoutException">Thrown when the elapsed time exceeds the specified timeout.</exception>
		/// <exception cref="LCTFBusyException">Thrown when the filter sends a busy interrupt to signify that it was busy and can't handle the request at this time.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the filter throws an error.</exception>
		public Task<int> WaitForTune(int timeout = 30000)
		{
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

			OnErrorHandler errorHandler = null;
			OnBusyHandler busyHandler = null;
			OnTuningDoneHandler tuningDoneHandler = null;

			// Hook up for error events
			this.OnError += errorHandler = (state, lambda) =>
			{
				this.OnError -= errorHandler;
				tcs.TrySetException(new InvalidOperationException("LCTF threw an error while trying to tune."));
			};

			// Hook up for busy events
			this.OnBusy += busyHandler = (state, lambda) =>
			{
				this.OnBusy -= busyHandler;
				tcs.TrySetException(new LCTFBusyException("LCTF was busy and not able to handle the last request."));
			};

			// Hook up for tuning done events
			this.OnTuningDone += tuningDoneHandler = (lambda) =>
			{
				// Remove the handler so we don't run on the next one
				this.OnTuningDone -= tuningDoneHandler;
				tcs.TrySetResult(lambda);
			};

			Task.Delay(timeout).ContinueWith((delayTask) =>
			{
				this.OnError -= errorHandler;
				this.OnTuningDone -= tuningDoneHandler;
				tcs.TrySetException(new TimeoutException($"{nameof(WaitForTune)} timed out after {timeout}ms"));
			});

			return tcs.Task;
		}

		private void Reader_DataReceived(object sender, EndpointDataEventArgs e)
		{
			InterruptType type = (InterruptType)e.Buffer[0];
			LCTFState state = (LCTFState)e.Buffer[1];
			int wavelength = Convert.ToInt32(BitConverter.ToSingle(e.Buffer, 4));

			Task.Run(() =>
			{
				//_logger.Debug(m => m($"{interrupt.Type}"));

				switch (type)
				{
					case InterruptType.Error:
						var onError = OnError;
						onError?.Invoke(state, wavelength);
						break;
					case InterruptType.TuningDone:
						var onTuningDone = OnTuningDone;
						onTuningDone?.Invoke(wavelength);
						break;
					case InterruptType.CalibrationDone:
						var onCalibrationDone = OnCalibrationDone;
						onCalibrationDone?.Invoke();
						break;
					case InterruptType.StateChanged:
						var onStateChanged = OnStateChanged;
						onStateChanged?.Invoke(state, wavelength);
						break;
					case InterruptType.Busy:
						var onBusy = OnBusy;
						onBusy?.Invoke(state, wavelength);
						break;
					default:
						break;
				}
			});
		}

		private void UsbDevice_UsbErrorEvent(object sender, UsbError e)
		{
			//_logger.Warn($"USB error: {e.Description}, code: {e.Win32ErrorNumber}, {e.Win32ErrorString}");
		}

		~LCTFDevice()
		{
			_usbDevice?.Close();
		}

		public void Dispose()
		{
			_interruptReader?.Abort();
			_interruptReader?.Dispose();
			_usbDevice?.Close();
		}

		private string GetSerial()
		{
			return _usbDevice.Info.SerialString;
		}

		internal void SetParameter(byte index, float value)
		{
			var usbPacket = NewSetPacket(index);

			var buf = BitConverter.GetBytes(value);

			usbPacket.Length = (short)buf.Length;

			int transferred = 0;

			ControlTransfer(ref usbPacket, buf, buf.Length, out transferred);
		}

		internal void SetParameter(byte index, bool value)
		{
			var usbPacket = NewSetPacket(index);
			usbPacket.Value = Convert.ToInt16(value);

			int transferred = 0;

			ControlTransfer(ref usbPacket, null, 0, out transferred);
		}

		internal byte GetByte(byte index, byte value = 0)
		{
			var usbPacket = NewGetPacket(index);
			usbPacket.Value = value;

			byte[] returnValue = { 0x00 };

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(sizeof(byte));

			int transferred = 0;

			ControlTransfer(ref usbPacket, unmanagedPointer, sizeof(byte), out transferred);

			Marshal.Copy(unmanagedPointer, returnValue, 0, 1);
			Marshal.FreeHGlobal(unmanagedPointer);

			return returnValue[0];
		}

		internal float GetFloat(byte index, byte value = 0)
		{
			var usbPacket = NewGetPacket(index);
			usbPacket.Value = value;

			float[] returnValue = { 0f };

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(sizeof(float));

			int transferred = 0;

			ControlTransfer(ref usbPacket, unmanagedPointer, sizeof(float), out transferred);

			Marshal.Copy(unmanagedPointer, returnValue, 0, 1);
			Marshal.FreeHGlobal(unmanagedPointer);

			return returnValue[0];
		}

		internal static UsbSetupPacket NewGetPacket(short index)
		{
			return new UsbSetupPacket(0b11000000, 0x80, 0, index, 0);
		}

		internal static UsbSetupPacket NewSetPacket(short index)
		{
			return new UsbSetupPacket(0b1000000, 0x81, 0, index, 0);
		}

		internal void ControlTransfer(ref UsbSetupPacket usbPacket, object buffer, int length, out int transferred)
		{
			bool success = false;

			try
			{
				success = _usbDevice.ControlTransfer(ref usbPacket, buffer, length, out transferred);

				if (!success)
				{
					var errorNum = UsbDevice.LastErrorNumber;
					var errorString = UsbDevice.LastErrorString;

					throw new InvalidOperationException($"USB communication failed. Error number: {errorNum}, {errorString}");
				}
			}
			catch (ObjectDisposedException)
			{
				throw new InvalidOperationException("USB communication failed.");
			}
		}
	}

	/// <summary>
	/// Handler for tuning done event.
	/// </summary>
	/// <param name="wavelength">The wavelength that was tuned to.</param>
	public delegate void OnTuningDoneHandler(int wavelength);

	/// <summary>
	/// Handler for calibration done event.
	/// </summary>
	public delegate void OnCalibrationDoneHandler();

	/// <summary>
	/// Handler for state changed event.
	/// </summary>
	/// <param name="status">The new McfState.</param>
	/// <param name="tunedWavelength">The currently tuned wavelength.</param>
	public delegate void OnStateChangedHandler(LCTFState status, int tunedWavelength);

	/// <summary>
	/// Handler for when the filter has an error.
	/// </summary>
	public delegate void OnErrorHandler(LCTFState status, int lastTunedWavelength);

	/// <summary>
	/// Handler for when the filter was busy and couldn't handle command.
	/// </summary>
	public delegate void OnBusyHandler(LCTFState status, int lastTunedWavelength);
}
