// <copyright file="Enums.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

namespace ChemImage.LCTF
{
	/// <summary>
	/// States that the LCTF can be in.
	/// </summary>
	public enum LCTFState
	{
		/// <summary>
		/// The LCTF does not currently have a state. This should never happen.
		/// </summary>
		None = 0,

		/// <summary>
		/// The LCTF is tuned to a wavelength and ready for commands.
		/// </summary>
		Ready = 2,

		/// <summary>
		/// The LCTF is busy tuning or doing something else.
		/// </summary>
		Busy = 3,

		/// <summary>
		/// The LCTF is tuning to a wavelength.
		/// </summary>
		Tuning = 4,

		/// <summary>
		/// The LCTF is calibrating internal voltages.
		/// </summary>
		Calibrating = 5,
	}

	/// <summary>
	/// Types of interrupts that can be send by the LCTF.
	/// </summary>
	internal enum InterruptType
	{
		/// <summary>
		/// An error has occurred.
		/// </summary>
		Error = 0,

		/// <summary>
		/// Tuning to a wavelength has completed.
		/// </summary>
		TuningDone = 1,

		/// <summary>
		/// Calibration has completed.
		/// </summary>
		CalibrationDone = 3,

		/// <summary>
		/// The state of the LCTF has changed.
		/// </summary>
		StateChanged = 4,

		/// <summary>
		/// A command was not processed because the LCTF was busy when it was received.
		/// </summary>
		Busy = 5,
	}

	/// <summary>
	/// USB commands that can be sent to the LCTF.
	/// </summary>
	internal enum CommandIndices
	{
		/// <summary>
		/// Used to enable and disable LCTF internal voltages.
		/// </summary>
		FilterEnable = 0x00,

		/// <summary>
		/// Used to get the LCTF state.
		/// </summary>
		LCTFState = 0x01,

		/// <summary>
		/// Used to enable and disable auto tuning with changing temperature.
		/// </summary>
		AutotuneEnable = 0x03,

		/// <summary>
		/// Used to get the minimum wavelength of the LCTF.
		/// </summary>
		WavelengthMin = 0x10,

		/// <summary>
		/// Used to get the maximum wavelength of the LCTF.
		/// </summary>
		WavelengthMax = 0x11,

		/// <summary>
		/// Used to get the step size between wavelengths for the LCTF.
		/// </summary>
		WavelengthStep = 0x12,

		/// <summary>
		/// Used to set the tuned wavelength of the LCTF.
		/// </summary>
		SetWavelength = 0x13,

		/// <summary>
		/// Used to get the internal temperature of the LCTF.
		/// </summary>
		GetTemperature = 0x21,

		/// <summary>
		/// Used to start calibration of the LCTF internal voltages.
		/// </summary>
		Calibrate = 0x32,

		/// <summary>
		/// Used to enable or disable overdrive.
		/// </summary>
		OverdriveEnable = 0xF9,
	}
}
