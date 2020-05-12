# ChemImageLCTF

CI Status: <a href="https://github.com/aklein53/ChemImageLCTF/actions?query=workflow%3ABuild"><img alt="GitHub Actions Build Status" src="https://github.com/aklein53/ChemImageLCTF/workflows/Build/badge.svg"></a>
## Overview
ChemImageLCTF is a .NET Framework library that can be used to control ChemImage Liquid Crystal Tunable Filters (LCTFs) over USB. It uses [LibUsbDotNet](https://github.com/LibUsbDotNet/LibUsbDotNet) for the USB communications.
## Installation
### NuGet Package
This library is available as a NuGet package and can be installed through the Visual Studio package manager o via the nuget package manager CLI by typing:

    nuget install ChemImageLCTF
### ChemImage LCTF SDK
This library is installed as part of the [ChemImage LCTF SDK](https://github.com/ChemImageFT/ChemImageLctfSdk). The library is then located in the install directory and can be referenced from there.
## Basic Usage
### LCTFController
This class monitors and manages connections to LCTF devices. To connect to an LCTF device, you can call the GetFirstLCTF function to get an LCTFDevice object for that device. Alternative, you can use the AttachedLCTFs property to access all LCTFDevice objects currently available.
LCTFController also provides two events, OnLctfAttached and OnLctfDetached, which fire whenever a device is connected or disconnected.
### LCTFDevice
This class represents a single LCTF device. 
#### Properties
| Property | Description |
|--|--|
| InstanceID | The USB DevicePath from Windows. This can be used to distinguish multiple devices connected  at once. |
| WavelengthMin | The minimum wavelength that the LCTF an be tuned to.|
| WavelengthMax | The maximum wavelength that the LCTF an be tuned to.|
| WavelengthStep | The step size between tunable wavelengths.|
#### Functions
| Function | Description |
|--|--|
| GetTemperature| Returns the internal temperature of the LCTF in degrees Celsius.| 
| SetWavelength | Starts tuning the LCTF to a specified wavelength. This function returns upon the command being sent to the LCTF. The LCTF is not finished tuning until the OnTuningDone event fires. |
| SetWavelengthAsync | Tunes the LCTF to a specified wavelength and waits for OnTuningDone to fire before returning. |
| WaitForTune | Returns when the OnTuningDone event next fires. This can be used in conjunction with SetWavelength for situations where SetWavelengthAsync cannot be used. |
| GetState | Returns the current state of the LCTF. This can be Calibrating, Tuning, or Ready.|
#### Events
| Event | Description |
|--|--|
| OnTuningDone | This event is fired every time the LCTF finishes tuning to a wavelength. | 
| OnCalibrationDone | On startup, the LCTF goes through an internal calibration during which the LCTF cannot be operated. This event is fired when that calibration is completed. |
| OnStateChanged | This event is fired whenever the internal state of the LCTF changes. |
| OnBusy | This event is fired when the LCTF is unable to handle a command. This is usually due to trying to tune the LCTF when it is in the Calibrating or Tuning state. |

## Example Code
Example projects are included as part of the [SDK](https://github.com/ChemImageFT/ChemImageLctfSdk/tree/master/src).

A very simple example program is also shown below:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChemImage.LCTF;

namespace HyperspectralImageCapture
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Connecting to LCTF");
			var lctf = LCTFController.GetFirstLCTF();

			if (lctf == null)
			{
				Console.WriteLine("No LCTF available. Press any key to exit.");
				Console.ReadKey();
				return;
			}

			Console.WriteLine("Starting hyperspectral capture");
			var min = lctf.WavelengthMin;
			var max = lctf.WavelengthMax;
			var step = lctf.WavelengthStep;
			for (var currentWavelength = min; currentWavelength <= max; currentWavelength += step)
			{
				// We're in a non-async function, so we'll use Wait() instead of await
				lctf.SetWavelengthAsync(currentWavelength).Wait();
				Console.WriteLine($"Tuned to {currentWavelength} nm");

				// Insert your image capture code here
			}

			// The lctf object needs to be disposed
			lctf.Dispose();

			Console.WriteLine("Hyperspectral capture completed. Press any key to exit.");
			Console.ReadKey();
			return;
		}
	}
}
```
## Licensing
This project is licensed under the [MIT License](LICENSE). Copyright (c) 2020 ChemImage Corporation.
LibUsbDotNet is licensed under the [LGPL v3.0 License](https://github.com/LibUsbDotNet/LibUsbDotNet/blob/master/LICENSE). Copyright (c) 2006-2010 Travis Robinson. All rights reserved.

