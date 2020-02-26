// <copyright file="LCTFDevice.Events.cs" company="ChemImage Corporation">
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
	/// An LCTF device that acts as a bandpass filter at a specified wavelength.
	/// </summary>
	public partial class LCTFDevice
	{
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
	}
}
