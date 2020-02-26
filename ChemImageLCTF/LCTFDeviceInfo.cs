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
		public String SerialNumber { get; internal set; }
		public ushort FirmwareVersion { get; internal set; }
		public uint FlashIds { get; internal set; }
	};
}
