// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

using ALBRT.overlay.cs.Interfaces;
using System;

namespace ALBRT.overlay.cs.Events
{
	internal static class ALBRTManagerEvent
	{
		/// <summary>
		/// Remember to unsubscribe!
		/// </summary>
		public static event EventHandler<ALBRTManagerEventArgs> OnEvent;

		public static void Invoke(object o, ALBRTManagerEventArgs a) // our own invoke method so we can check before invoking the event
		{
			if (o is not IALBRTManagerEventSender) return;
			OnEvent?.Invoke(o, a);
		}
	}

	/// <summary>
	/// An set of events raiased by ALBRT that are globally relevant; including OpenVR API events, initialisation events, errors
	/// </summary>
	public enum ALBRTManagerEventType
	{
		NONE = 0, // reserved

		ALBRT_LOADING = 1,
		ALBRT_STARTED = 2,
		ALBRT_ERROR = 3,
		ALBRT_QUIT = 4,

		// OpenVR events

		VR_DASHBOARD_OPENED = 101,
		VR_DASHBOARD_CLOSED = 102,
	}

	public class ALBRTManagerEventArgs : EventArgs
	{
		public ALBRTManagerEventType type = ALBRTManagerEventType.NONE;
		public ALBRTManagerEventError printError;
	}

	/// <summary>
	/// Human readable error state passed to a UI; DO NOT USE for handling errors
	/// </summary>
	public struct ALBRTManagerEventError
	{
		public string error;
		public string solution;
	}
}
