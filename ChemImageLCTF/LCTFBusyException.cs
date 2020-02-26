// <copyright file="LCTFBusyException.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

using System;

namespace ChemImage.LCTF
{
	/// <summary>
	/// Exception for when an LCTF is unable to complete a command due to being busy doing something else.
	/// Problem is probably caused by not waiting for a tune to complete.
	/// </summary>
	[Serializable]
	public class LCTFBusyException : InvalidOperationException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LCTFBusyException"/> class.
		/// </summary>
		public LCTFBusyException()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LCTFBusyException"/> class.
		/// </summary>
		/// <param name="message">A message detailing the exception.</param>
		public LCTFBusyException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LCTFBusyException"/> class.
		/// </summary>
		/// <param name="message">A message detailing the exception.</param>
		/// <param name="innerException">The exception leading to this exception.</param>
		public LCTFBusyException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LCTFBusyException"/> class.
		/// </summary>
		/// <param name="serializationInfo">Info on how to serialize the object.</param>
		/// <param name="streamingContext">Info on the serialization stream.</param>
		protected LCTFBusyException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
		}
	}
}
