// <copyright file="LCTFDevice.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace ChemImage.LCTF
{
	/// <summary>
	/// An LCTF device that acts as a bandpass filter at a specified wavelength.
	/// </summary>
	public partial class LCTFDevice : IDisposable
	{
		private readonly UsbDevice usbDevice;
		private readonly UsbEndpointReader interruptReader;

		/// <summary>
		/// Initializes a new instance of the <see cref="LCTFDevice"/> class.
		/// </summary>
		/// <param name="underlyingWinUsbDevice">The underlying <see cref="UsbDevice"/>.</param>
		public LCTFDevice(UsbDevice underlyingWinUsbDevice)
		{
			this.usbDevice = underlyingWinUsbDevice ?? throw new ArgumentNullException(nameof(underlyingWinUsbDevice));

			this.InstanceId = underlyingWinUsbDevice.DevicePath;

			this.DeviceInfo = this.GetDeviceInfo();

			if (this.DeviceInfo.FirmwareVersion < 107)
			{
				throw new NotSupportedException("This library only supports LCTF firmware v1.07 or newer and cannot be used with older versions.");
			}

			this.WavelengthMin = (int)(this.GetFloat((byte)CommandIndices.WavelengthMin) + 0.5f);
			this.WavelengthMax = (int)(this.GetFloat((byte)CommandIndices.WavelengthMax) + 0.5f);
			this.WavelengthStep = (int)(this.GetFloat((byte)CommandIndices.WavelengthStep) + 0.5f);

			UsbDevice.UsbErrorEvent += this.UsbDevice_UsbErrorEvent;

			// Set up reader for interrupts
			this.interruptReader = this.usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
			this.interruptReader.DataReceivedEnabled = true;
			this.interruptReader.DataReceived += this.Reader_DataReceived;

			// Turn on all the normal features
			this.SetFilterEnable(true);
			this.SetAutotune(true);
			this.SetOUStatus(true);
		}

		/// <summary>
		/// Gets the underlying USB DevicePath.
		/// </summary>
		public string InstanceId { get; private set; }

		/// <summary>
		/// Gets static information about the device.
		/// </summary>
		public LCTFDeviceInfo DeviceInfo { get; private set; }

		/// <summary>
		/// Gets the minimum wavelength the LCTF can tune to.
		/// </summary>
		public int WavelengthMin { get; private set; }

		/// <summary>
		/// Gets the maximum wavelength the LCTF can tune to.
		/// </summary>
		public int WavelengthMax { get; private set; }

		/// <summary>
		/// Gets the step size between tuneable wavelengths.
		/// </summary>
		public int WavelengthStep { get; private set; }

		/// <summary>
		/// Gets the currently tuned wavelength of the LCTF.
		/// </summary>
		public LCTFState GetCurrentWavelength()
		{
			return (LCTFState)this.GetByte((byte)CommandIndices.SetWavelength);
		}

		/// <summary>
		/// Gets the current state of the LCTF.
		/// </summary>
		public LCTFState GetState()
		{
			return (LCTFState)this.GetByte((byte)CommandIndices.LCTFState);
		}

		/// <summary>
		/// Gets the current internal temperature of the LCTF.
		/// </summary>
		/// <returns>The temperature in degrees C.</returns>
		public float GetTemperature()
		{
			var temp = this.GetFloat((byte)CommandIndices.GetTemperature);
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

			this.SetParameter((byte)CommandIndices.SetWavelength, wavelength);
		}

		/// <summary>
		/// Sets the LCTF to a specified wavelength and returns after <see cref="OnTuningDone"/> fires.
		/// </summary>
		/// <param name="wavelength">The wavelength to tune to.</param>
		/// <returns>The wavelength that was set.</returns>
		public async Task<int> SetWavelengthAsync(int wavelength)
		{
			Task<int> tuneTask = WaitForTune(1000);
			this.SetWavelength(wavelength);

			return await tuneTask.ConfigureAwait(false);
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

			Task.Delay(timeout).ContinueWith(
			(delayTask) =>
			{
				this.OnError -= errorHandler;
				this.OnTuningDone -= tuningDoneHandler;
				tcs.TrySetException(new TimeoutException($"{nameof(this.WaitForTune)} timed out after {timeout}ms"));
			}, TaskScheduler.Default);

			return tcs.Task;
		}

		/// <summary>
		/// Gets information about the device. Includes serial number and firmware version.
		/// </summary>
		/// <returns>An <see cref="LCTFDeviceInfo"/>.</returns>
		private LCTFDeviceInfo GetDeviceInfo()
		{
			LCTFDeviceInfo deviceInfo = new LCTFDeviceInfo
			{
				SerialNumber = this.GetSerial()
			};

			var bcdVersion = (ushort)this.usbDevice.Info.Descriptor.BcdDevice;
			deviceInfo.FirmwareVersion = (ushort)((((bcdVersion >> 8) & 0x00ff) * 100) + (bcdVersion & 0x00ff));

			return deviceInfo;
		}

		/// <summary>
		/// Turns the internal voltages on and off. These must be on for the LCTF to function and should only be turned off for testing purposes.
		/// </summary>
		/// <param name="status">True to turn on the filter, false to turn off.</param>
		private void SetFilterEnable(bool status)
		{
			this.SetParameter((byte)CommandIndices.FilterEnable, status);
		}

		/// <summary>
		/// Turns the autotune function on and off. This will adjust internal voltages to maintain the passband as temperature changes.
		/// </summary>
		/// <param name="status">True to turn on autotune, false to turn off.</param>
		private void SetAutotune(bool status)
		{
			this.SetParameter((byte)CommandIndices.AutotuneEnable, status);
		}

		/// <summary>
		/// Turns the overdrive function on and off. This allows the filter to tune more quickly and should only be turned off for testing purposes.
		/// </summary>
		/// <param name="status">True to turn on overdrive, false to turn off.</param>
		private void SetOUStatus(bool status)
		{
			this.SetParameter((byte)CommandIndices.OverdriveEnable, status);
		}

		private void Reader_DataReceived(object sender, EndpointDataEventArgs e)
		{
			InterruptType type = (InterruptType)e.Buffer[0];
			LCTFState state = (LCTFState)e.Buffer[1];
			int wavelength = Convert.ToInt32(BitConverter.ToSingle(e.Buffer, 4));

			Task.Run(() =>
			{
				// _logger.Debug(m => m($"{interrupt.Type}"));
				switch (type)
				{
					case InterruptType.Error:
						var onError = this.OnError;
						onError?.Invoke(state, wavelength);
						break;
					case InterruptType.TuningDone:
						var onTuningDone = this.OnTuningDone;
						onTuningDone?.Invoke(wavelength);
						break;
					case InterruptType.CalibrationDone:
						var onCalibrationDone = this.OnCalibrationDone;
						onCalibrationDone?.Invoke();
						break;
					case InterruptType.StateChanged:
						var onStateChanged = this.OnStateChanged;
						onStateChanged?.Invoke(state, wavelength);
						break;
					case InterruptType.Busy:
						var onBusy = this.OnBusy;
						onBusy?.Invoke(state, wavelength);
						break;
					default:
						break;
				}
			});
		}

		private void UsbDevice_UsbErrorEvent(object sender, UsbError e)
		{
			// _logger.Warn($"USB error: {e.Description}, code: {e.Win32ErrorNumber}, {e.Win32ErrorString}");
		}

		private string GetSerial()
		{
			return this.usbDevice.Info.SerialString;
		}
	}
}