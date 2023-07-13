// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

using ALBRT.overlay.cs.Interfaces;
using System;

namespace ALBRT.overlay.cs.Events
{
	internal static class EyeOverlaysEvent
	{
		/// <summary>
		/// Remember to unsubscribe!
		/// </summary>
		public static event EventHandler<EyeOverlaysEventArgs> OnChange;

		public static void Invoke(object o, EyeOverlaysEventArgs a) // our own invoke method so we can check before invoking the event
		{
			if (o is not IEyeOverlaysEventSender) return;
			OnChange?.Invoke(o, a);
		}

		/// <summary>
		/// Use to invoke INIT for ALL properties so some observer can init or refresh values - use to refresh whole UIs and sync values - use a string filter for specificity
		/// </summary>
		/// <param name="type">An alternative type to invoke</param>
		/// <param name="filter">A string filter for observers. Default is "init"</param>
		public static void InvokeAll(object o, EyeOverlaysEventType type = EyeOverlaysEventType.INIT, string filter = "init")
		{
			if (o is not IEyeOverlaysEventSender) return;
			foreach (int i in Enum.GetValues(typeof(EyeOverlaysEventProperty)))
				{
				Invoke(o, new EyeOverlaysEventArgs
				{
					property = (EyeOverlaysEventProperty)i,
					type = type,
					filter = filter
				});
			}
		}
	}
	
	// TODO will want to split off the pure flags from the properties if more pure flags are utilised

	/// <summary>
	/// Observed properties relating to eye overlays that will report dirty/cleared/init
	/// </summary>
	public enum EyeOverlaysEventProperty
	{
		// NOTE you can name these in any way, but it is easier to just use the property name from the classes that will utilise these events

		NONE = 0, // reserved

		// dirty flag properties

		IPD_CHANGED,

		// properties with dirty flags

		VirtualDistance,
		Alpha,
		OverlayMaskType,
		PatchColour,
		PatchType,
		PatchRadialSize,
		PatchRadialSoftness,
		SlatColour,
		SlatHeight,
		SlatSliceHeight,
		SlatSliceOffset,
		EyeOverlaysHideInDash,
		EyeOverlaysVisible,
		EyeToRender,
		EyeOverlaysSwitched,
		AlphaTEnabled,
		AlphaTSpeed,
		AlphaTType,
		FogColour,
		FogAnimated,
		FogSpeed,
		FogDirection,
		FogType,
		FogSeed,
	}

	public enum EyeOverlaysEventType
	{
		/// <summary>
		/// Reserved
		/// </summary>
		NONE = 0, // reserved 

		/// <summary>
		/// Initialisation & Refreshing - use this for data init or wholesale refreshing of UI, etc
		/// </summary>
		INIT = 1,

		/// <summary>
		/// Value has changed - automatically sent upon changing property value - only act on this internally to resolve data changes
		/// </summary>
		DIRTY = 2,

		/// <summary>
		/// Value change was resolved - use this to update UI for single properties, etc
		/// </summary>
		CLEARED = 3,
	}

	public class EyeOverlaysEventArgs : EventArgs
	{
		/// <summary>
		/// The property key from EyeOverlaysEventProperty
		/// </summary>
		public EyeOverlaysEventProperty property = EyeOverlaysEventProperty.NONE;

		/// <summary>
		/// The type of event from EyeOverlaysEventType
		/// </summary>
		public EyeOverlaysEventType type = EyeOverlaysEventType.NONE;

		/// <summary>
		/// A string filter for observers
		/// </summary>
		public string filter;
	}
}
