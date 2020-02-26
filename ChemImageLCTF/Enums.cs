using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChemImage.LCTF
{
	/// <summary>
	/// States that the MCF can be in.
	/// </summary>
	public enum LCTFState
	{
		None = 0,
		Ready = 2,
		Busy = 3,
		Tuning = 4,
		Calibrating = 5,
	};

	internal enum InterruptType
	{
		Error = 0,
		TuningDone = 1,
		CalibrationDone = 3,
		StateChanged = 4,
		Busy = 5,
	};

	internal enum CommandIndices
	{
		FilterEnable = 0x00,
		LCTFState = 0x01,
		WavelengthMin = 0x10,
		WavelengthMax = 0x11,
		WavelengthStep = 0x12,
		SetWavelength = 0x13,
		GetTemperature = 0x21,
		Calibrate = 0x32,

	}
}
