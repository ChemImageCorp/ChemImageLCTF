// <copyright file="LCTFDevice.LowLevel.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace ChemImage.LCTF
{
	/// <summary>
	/// An LCTF device that acts as a bandpass filter at a specified wavelength.
	/// </summary>
	public partial class LCTFDevice
	{
		private static UsbSetupPacket NewGetPacket(short index)
		{
			return new UsbSetupPacket(0b11000000, 0x80, 0, index, 0);
		}

		private static UsbSetupPacket NewSetPacket(short index)
		{
			return new UsbSetupPacket(0b1000000, 0x81, 0, index, 0);
		}

		private void SetParameter(byte index, float value)
		{
			var usbPacket = NewSetPacket(index);

			var buf = BitConverter.GetBytes(value);

			usbPacket.Length = (short)buf.Length;

			this.ControlTransfer(ref usbPacket, buf, buf.Length, out _);
		}

		private void SetParameter(byte index, bool value)
		{
			var usbPacket = NewSetPacket(index);
			usbPacket.Value = Convert.ToInt16(value);

			this.ControlTransfer(ref usbPacket, null, 0, out _);
		}

		private byte GetByte(byte index, byte value = 0)
		{
			var usbPacket = NewGetPacket(index);
			usbPacket.Value = value;

			byte[] returnValue = { 0x00 };

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(sizeof(byte));

			this.ControlTransfer(ref usbPacket, unmanagedPointer, sizeof(byte), out _);

			Marshal.Copy(unmanagedPointer, returnValue, 0, 1);
			Marshal.FreeHGlobal(unmanagedPointer);

			return returnValue[0];
		}

		private float GetFloat(byte index, byte value = 0)
		{
			var usbPacket = NewGetPacket(index);
			usbPacket.Value = value;

			float[] returnValue = { 0f };

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(sizeof(float));

			this.ControlTransfer(ref usbPacket, unmanagedPointer, sizeof(float), out _);

			Marshal.Copy(unmanagedPointer, returnValue, 0, 1);
			Marshal.FreeHGlobal(unmanagedPointer);

			return returnValue[0];
		}

		private void ControlTransfer(ref UsbSetupPacket usbPacket, object buffer, int length, out int transferred)
		{
			try
			{
				bool success = this.usbDevice.ControlTransfer(ref usbPacket, buffer, length, out transferred);

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
}
