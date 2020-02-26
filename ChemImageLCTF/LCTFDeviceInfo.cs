// <copyright file="LCTFDeviceInfo.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChemImage.LCTF
{
	/// <summary>
	/// Contains static information about the LCTF device.
	/// </summary>
	public class LCTFDeviceInfo
	{
		/// <summary>
		/// Gets the serial number of the LCTF.
		/// </summary>
		public string SerialNumber { get; internal set; }

		/// <summary>
		/// Gets the firmware version of the LCTF.
		/// </summary>
		public ushort FirmwareVersion { get; internal set; }
	}
}
