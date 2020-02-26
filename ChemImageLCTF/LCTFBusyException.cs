using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChemImage.LCTF
{
	/// <summary>
	/// Exception for when an LCTF is unable to complete a command due to being busy doing something else.
	/// Problem is probably caused by not waiting for a tune to complete.
	/// </summary>
	public class LCTFBusyException : InvalidOperationException
	{
		public LCTFBusyException(string message) : base(message)
		{

		}
	}
}
