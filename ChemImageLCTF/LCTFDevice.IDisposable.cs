// <copyright file="LCTFDevice.IDisposable.cs" company="ChemImage Corporation">
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
		private bool mDisposed = false;

		/// <summary>
		/// Finalizes an instance of the <see cref="LCTFDevice"/> class.
		/// </summary>
		~LCTFDevice()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Cleans up USB connections and disposes the object.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the <see cref="LCTFDevice"/>.
		/// </summary>
		/// <param name="disposeManagedResources">Indicates if managed resources should be disposed in addition to unmanaged.</param>
		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!this.mDisposed)
			{
				if (disposeManagedResources)
				{
					// Release managed resources
					this.interruptReader?.Abort();
					this.interruptReader?.Dispose();
					this.usbDevice?.Close();
				}

				// Release unmanaged resources
				this.mDisposed = true;
			}
		}
	}
}
