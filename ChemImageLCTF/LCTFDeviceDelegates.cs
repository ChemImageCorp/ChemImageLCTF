// <copyright file="LCTFDeviceDelegates.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

namespace ChemImage.LCTF
{
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
	/// <param name="state">The new LCFTState.</param>
	/// <param name="tunedWavelength">The currently tuned wavelength.</param>
	public delegate void OnStateChangedHandler(LCTFState state, int tunedWavelength);

	/// <summary>
	/// Handler for when the filter has an error.
	/// </summary>
	/// <param name="state">The LCFTState.</param>
	/// <param name="lastTunedWavelength">The currently tuned wavelength.</param>
	public delegate void OnErrorHandler(LCTFState state, int lastTunedWavelength);

	/// <summary>
	/// Handler for when the filter was busy and couldn't handle command.
	/// </summary>
	/// <param name="state">The LCFTState.</param>
	/// <param name="lastTunedWavelength">The currently tuned wavelength.</param>
	public delegate void OnBusyHandler(LCTFState state, int lastTunedWavelength);
}
