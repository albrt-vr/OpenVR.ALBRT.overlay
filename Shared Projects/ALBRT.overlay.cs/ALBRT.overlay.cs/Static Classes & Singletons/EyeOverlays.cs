// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

using System.IO;
using System.Numerics;
using ALBRT.overlay.cs.Events;
using ALBRT.overlay.cs.Interfaces;
using Newtonsoft.Json;

namespace ALBRT.overlay.cs.Data
{
	#region Public Enums

	/// <summary>
	/// Mask type - DO NOT EDIT - hard coded values used - if adding, use next int - do not delete deprecated
	/// </summary>
	public enum MaskType
	{
		NONE = 0, // reserved

		/// <summary>
		/// From an image file - no longer in use as authoring was too annoying
		/// </summary>
		IMAGE = 1,

		/// <summary>
		/// 2D Render a flat colour fill
		/// </summary>
		PATCH = 2,

		/// <summary>
		/// 2D Render reciprocal slats
		/// </summary>
		SLAT = 3,

		/// <summary>
		/// 3D Render a complex fog of war mask
		/// </summary>
		FOG = 4
	}

	/// <summary>
	/// Basic animation types for alpha
	/// </summary>
	public enum AnimationType
	{
		NONE = 0, // reserved

		A01 = 1,
		A10 = 2,
		APINGPONG = 3,
	}

	/// <summary>
	/// Patch types
	/// </summary>
	public enum PatchType
	{
		NONE = 0, // reserved

		FLAT = 1,
		RADIAL = 2,
	}

	/// <summary>
	/// Fog types
	/// </summary>
	public enum FogType
	{
		NONE = 0, // reserved

		CLOUDS = 1,
		GRID_WINDOWS = 2,
		DOT_WINDOWS = 3,
	}

	/// <summary>
	/// Eye - DO NOT EDIT - hard coded values used
	/// </summary>
	public enum Eye
	{
		NONE = 0, // reserved

		LEFT = 1,
		RIGHT = 2,
		BOTH = 3,
	}

	#endregion

	/// <summary>
	/// (Singleton)(EyeOverlays.Instance) Holds the state of the eye overlays for the lifetime of the app
	/// </summary>
	internal class EyeOverlays : IEyeOverlaysEventSender
	{
		#region Singleton

		private EyeOverlays() { } // is singleton - prevent constructor - access the class via the single Instance, see below

		//

		public static EyeOverlays Instance
		{
			// Singleton pattern for a class; Forces the use of the one and only single instance of this class type (instance) instead of using a new class instance
			// basically, this is a single thing for the single purpose in the lifecycle of the app, but it is not a pure static class as it needs to act as a class sometimes
			get
			{
				instance ??= new EyeOverlays();
				return instance;
			}
		}
		private static EyeOverlays instance; // do not access this interally - always access via Instance (capital I; the property)

		#endregion

		//

		private static readonly string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

		#region Constant Naming Fields

		/// <summary>
		/// The string key for the LEFT overlay
		/// </summary>
		public const string leftKey = "ALBRT.overlay.eye.left";

		/// <summary>
		/// The string key for the RIGHT overlay
		/// </summary>
		public const string rightKey = "ALBRT.overlay.eye.right";

		/// <summary>
		/// The string name, for humans, for the LEFT overlay
		/// </summary>
		public const string leftName = "ALBRT Left Eye";

		/// <summary>
		/// The string name, for humans, for the RIGHT overlay
		/// </summary>
		public const string rightName = "ALBRT Right Eye";

		#endregion

		#region Transform Fields

		/// <summary>
		/// (Metres)(From HMD right eye)(IPD/2) The right side delta from HMD X.zero to the right eye centre; left is minus this; *2 for IPD
		/// </summary>
		public static float x = float.MinValue; // runtime cache from HMD -- IMPORTANT the default must be some impossible value NOT an actual IPD value

		/// <summary>
		/// (Metres)(Always 0) Y axis
		/// </summary>
		public const float y = 0f; // always 0

		/// <summary>
		/// (Metres)(From OpenVR, the near clipping plane + 1mm) The distance of the overlay quads from the HMD eye origin in the forward(-)/backward(+) directions
		/// </summary>
		public const float z = -0.101f; // always min  -0.101f (near clipping plane + 1mm) && always negative as Z- is forward

		/// <summary>
		/// (Metres)(The distance corrected quad size) The width of the overlay square quads - ie becomes height too 
		/// </summary>
		public const float w = quadSize * (z * -1); // we can assume z is negative as in OpenVR Z- is forward

		/// <summary>
		/// (Metres) The desired square size of the overlay quads at 1m distance
		/// </summary>
		public const float quadSize = 2.4f;

		#endregion

		//

		#region Public Dirty Flag Properties

		/// <summary>
		/// (Dirty flag) The IPD value changed
		/// </summary>
		public static bool IPD_CHANGED
		{
			set
			{
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.IPD_CHANGED,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}

		#endregion

		#region Public Properties With Dirty Flags
		// IMPORTANT DO NOT set any of these properties during internal polling or you will cause recurrsion - they are for a UI/config to change

		/// <summary>
		/// (Sets dirty flag) The virtual distance to simulate the fog of war (stereo) overlays at
		/// </summary>
		public static float VirtualDistance
		{
			// NOTE I have removed the fog of war code until I can get everything done in OpenGL which will take some time
			// basically we will use a full scene in OpenGL, allowing us to render 3D stereo masks corectly via a render texture for each eye
			get { return virtualDistance; }
			set
			{
				virtualDistance = value;
				VirtualDistance_dirty = true;
			}
		}
		private static float virtualDistance;

		public static bool VirtualDistance_dirty
		{
			get { return virtualDistance_dirty; }
			set
			{
				virtualDistance_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.VirtualDistance,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool virtualDistance_dirty;

		/// <summary>
		/// (Sets dirty flag)(0-1) The alpha (Translucency) of the overlay quads (0 == invisible; 1 == opaque)
		/// </summary>
		[JsonProperty()]
		public static float Alpha
		{
			get { return alpha; }
			set
			{
				alpha = value;
				Alpha_dirty = true;
			}
		}
		private static float alpha = 1f;

		public static bool Alpha_dirty
		{
			get { return alpha_dirty; }
			set
			{
				alpha_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.Alpha,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool alpha_dirty;

		/// <summary>
		/// (Sets dirty flag) Which mask type are we currently using
		/// </summary>
		[JsonProperty()]
		public static MaskType OverlayMaskType
		{
			get { return overlayMaskType; }
			set
			{
				overlayMaskType = value;
				OverlayMaskType_dirty = true;
			}
		}
		private static MaskType overlayMaskType = MaskType.PATCH;

		public static bool OverlayMaskType_dirty
		{
			get { return overlayMaskType_dirty; }
			set
			{
				overlayMaskType_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.OverlayMaskType,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool overlayMaskType_dirty;

		/// <summary>
		/// (Sets dirty flag) The colour of the patch mask
		/// </summary>
		[JsonProperty()]
		public static Vector3 PatchColour
		{
			get { return patchColour; }
			set
			{
				patchColour = value;
				PatchColour_dirty = true;
			}
		}
		private static Vector3 patchColour = new(0f, 0f, 0f);

		public static bool PatchColour_dirty
		{
			get { return patchColour_dirty; }
			set
			{
				patchColour_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.PatchColour,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool patchColour_dirty;

		/// <summary>
		/// (Sets dirty flag) The patch type
		/// </summary>
		[JsonProperty()]
		public static PatchType PatchType
		{
			get { return patchType; }
			set
			{
				patchType = value;
				PatchType_dirty = true;
			}
		}
		private static PatchType patchType = PatchType.RADIAL;

		public static bool PatchType_dirty
		{
			get { return patchType_dirty; }
			set
			{
				patchType_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.PatchType,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool patchType_dirty;

		/// <summary>
		/// (Sets dirty flag) The radial patch size
		/// </summary>
		[JsonProperty()]
		public static float PatchRadialSize
		{
			get { return patchRadialSize; }
			set
			{
				patchRadialSize = value;
				PatchRadialSize_dirty = true;
			}
		}
		private static float patchRadialSize = 0.05f;

		public static bool PatchRadialSize_dirty
		{
			get { return patchRadialSize_dirty; }
			set
			{
				patchRadialSize_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.PatchRadialSize,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool patchRadialSize_dirty;

		/// <summary>
		/// (Sets dirty flag) The radial patch edge softness
		/// </summary>
		[JsonProperty()]
		public static float PatchRadialSoftness
		{
			get { return patchRadialSoftness; }
			set
			{
				patchRadialSoftness = value;
				PatchRadialSoftness_dirty = true;
			}
		}
		private static float patchRadialSoftness = 0.2f;

		public static bool PatchRadialSoftness_dirty
		{
			get { return patchRadialSoftness_dirty; }
			set
			{
				patchRadialSoftness_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.PatchRadialSoftness,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool patchRadialSoftness_dirty;

		/// <summary>
		/// (Sets dirty flag) The colour of the slats mask
		/// </summary>
		[JsonProperty()]
		public static Vector3 SlatColour
		{
			get { return slatColour; }
			set
			{
				slatColour = value;
				SlatColour_dirty = true;
			}
		}
		private static Vector3 slatColour = new(0f, 0f, 0f);

		public static bool SlatColour_dirty
		{
			get { return slatColour_dirty; }
			set
			{
				slatColour_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.SlatColour,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool slatColour_dirty;

		/// <summary>
		/// (Sets dirty flag) % Of the slice (a slat + gap) that is the slat vs gap
		/// </summary>
		[JsonProperty()]
		public static float SlatHeight
		{
			get { return slatHeight; }
			set
			{
				slatHeight = value;
				SlatHeight_dirty = true;
			}
		}
		private static float slatHeight = 0.5f;

		public static bool SlatHeight_dirty
		{
			get { return slatHeight_dirty; }
			set
			{
				slatHeight_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.SlatHeight,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool slatHeight_dirty;

		/// <summary>
		/// (Sets dirty flag) % Of the viewport that a slice (a slat + gap) fills - lower means more slices to fill the viewport
		/// </summary>
		[JsonProperty()]
		public static float SlatSliceHeight
		{
			get { return slatSliceHeight; }
			set
			{
				slatSliceHeight = value;
				SlatSliceHeight_dirty = true;
			}
		}
		private static float slatSliceHeight = 0.1f;

		public static bool SlatSliceHeight_dirty
		{
			get { return slatSliceHeight_dirty; }
			set
			{
				slatSliceHeight_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.SlatSliceHeight,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool slatSliceHeight_dirty;

		/// <summary>
		/// (Sets dirty flag) An offset for the slices to suit the user's needs
		/// </summary>
		[JsonProperty()]
		public static float SlatSliceOffset
		{
			get { return slatSliceOffset; }
			set
			{
				slatSliceOffset = value;
				SlatSliceOffset_dirty = true;
			}
		}
		private static float slatSliceOffset = 0f;

		public static bool SlatSliceOffset_dirty
		{
			get { return slatSliceOffset_dirty; }
			set
			{
				slatSliceOffset_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.SlatSliceOffset,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool slatSliceOffset_dirty;

		/// <summary>
		/// (Sets dirty flag) Hide the overlays when viewing the SteamVR dashboard?
		/// </summary>
		[JsonProperty()]
		public static bool EyeOverlaysHideInDash
		{
			get { return eyeOverlaysHideInDash; }
			set
			{
				eyeOverlaysHideInDash = value;
				EyeOverlaysHideInDash_dirty = true;
			}
		}
		private static bool eyeOverlaysHideInDash = true;

		public static bool EyeOverlaysHideInDash_dirty
		{
			get { return eyeOverlaysHideInDash_dirty; }
			set
			{
				eyeOverlaysHideInDash_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.EyeOverlaysHideInDash,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool eyeOverlaysHideInDash_dirty;

		/// <summary>
		/// (Sets dirty flag) Are the eye overlays visible?
		/// </summary>
		[JsonProperty()]
		public static bool EyeOverlaysVisible
		{
			get { return eyeOverlaysVisible; }
			set
			{
				eyeOverlaysVisible = value;
				EyeOverlaysVisible_dirty = true;
			}
		}
		private static bool eyeOverlaysVisible = true;

		public static bool EyeOverlaysVisible_dirty
		{
			get { return eyeOverlaysVisible_dirty; }
			set
			{
				eyeOverlaysVisible_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.EyeOverlaysVisible,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool eyeOverlaysVisible_dirty;

		/// <summary>
		/// (Sets dirty flag) Which eye are we rendering? Or both?
		/// </summary>
		[JsonProperty()]
		public static Eye EyeToRender
		{
			get { return eyeToRender; }
			set
			{
				eyeToRender = value;
				EyeToRender_dirty = true;
			}
		}
		private static Eye eyeToRender = Eye.BOTH;

		public static bool EyeToRender_dirty
		{
			get { return eyeToRender_dirty; }
			set
			{
				eyeToRender_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.EyeToRender,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool eyeToRender_dirty;

		/// <summary>
		/// (Sets dirty flag) Have the eyes been switched?
		/// </summary>
		[JsonProperty()]
		public static bool EyeOverlaysSwitched
		{
			get { return eyeOverlaysSwitched; }
			set
			{
				eyeOverlaysSwitched = value;
				EyeOverlaysSwitched_dirty = true;
			}
		}
		private static bool eyeOverlaysSwitched = false;

		public static bool EyeOverlaysSwitched_dirty
		{
			get { return eyeOverlaysSwitched_dirty; }
			set
			{
				eyeOverlaysSwitched_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.EyeOverlaysSwitched,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool eyeOverlaysSwitched_dirty;

		/// <summary>
		/// (Sets dirty flag) Is the alpha over time feature on?
		/// </summary>
		[JsonProperty()]
		public static bool AlphaTEnabled
		{
			get { return alphaTEnabled; }
			set
			{
				alphaTEnabled = value;
				AlphaTEnabled_dirty = true;
			}
		}
		private static bool alphaTEnabled = false;

		public static bool AlphaTEnabled_dirty
		{
			get { return alphaTEnabled_dirty; }
			set
			{
				alphaTEnabled_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.AlphaTEnabled,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool alphaTEnabled_dirty;

		/// <summary>
		/// (Sets dirty flag) The alpha over time speed - ie the duration of the animation in seconds
		/// </summary>
		[JsonProperty()]
		public static float AlphaTSpeed
		{
			get { return alphaTSpeed; }
			set
			{
				alphaTSpeed = value;
				AlphaTSpeed_dirty = true;
			}
		}
		private static float alphaTSpeed = 3f; // duration in seconds of the chosen animation

		public static bool AlphaTSpeed_dirty
		{
			get { return alphaTSpeed_dirty; }
			set
			{
				alphaTSpeed_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.AlphaTSpeed,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool alphaTSpeed_dirty;

		/// <summary>
		/// (Sets dirty flag) The alpha over time animation type
		/// </summary>
		[JsonProperty()]
		public static AnimationType AlphaTType
		{
			get { return alphaTType; }
			set
			{
				alphaTType = value;
				AlphaTType_dirty = true;
			}
		}
		private static AnimationType alphaTType = AnimationType.APINGPONG;

		public static bool AlphaTType_dirty
		{
			get { return alphaTType_dirty; }
			set
			{
				alphaTType_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.AlphaTType,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool alphaTType_dirty;

		/// <summary>
		/// (Sets dirty flag) The colour of the fog mask
		/// </summary>
		public static Vector4 FogColour
		{
			get { return fogColour; }
			set
			{
				fogColour = value;
				FogColour_dirty = true;
			}
		}
		private static Vector4 fogColour = new(0, 0, 0, 1);

		public static bool FogColour_dirty
		{
			get { return fogColour_dirty; }
			set
			{
				fogColour_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogColour,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogColour_dirty;

		/// <summary>
		/// (Sets dirty flag) Does the fog animate?
		/// </summary>
		public static bool FogAnimated
		{
			get { return fogAnimated; }
			set
			{
				fogAnimated = value;
				FogAnimated_dirty = true;
			}
		}
		private static bool fogAnimated = false;

		public static bool FogAnimated_dirty
		{
			get { return fogAnimated_dirty; }
			set
			{
				fogAnimated_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogAnimated,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogAnimated_dirty;

		/// <summary>
		/// (Sets dirty flag) Fog animation speed
		/// </summary>
		public static float FogSpeed
		{
			get { return fogSpeed; }
			set
			{
				fogSpeed = value;
				FogSpeed_dirty = true;
			}
		}
		private static float fogSpeed = 0.1f;

		public static bool FogSpeed_dirty
		{
			get { return fogSpeed_dirty; }
			set
			{
				fogSpeed_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogSpeed,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogSpeed_dirty;

		/// <summary>
		/// (Sets dirty flag) Fog animation direction
		/// </summary>
		public static Vector3 FogDirection
		{
			get { return fogDirection; }
			set
			{
				fogDirection = value;
				FogDirection_dirty = true;
			}
		}
		private static Vector3 fogDirection;

		public static bool FogDirection_dirty
		{
			get { return fogDirection_dirty; }
			set
			{
				fogDirection_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogDirection,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogDirection_dirty;

		/// <summary>
		/// (Sets dirty flag) Fog type
		/// </summary>
		public static FogType FogType
		{
			get { return fogType; }
			set
			{
				fogType = value;
				FogType_dirty = true;
			}
		}
		private static FogType fogType = FogType.CLOUDS;

		public static bool FogType_dirty
		{
			get { return fogType_dirty; }
			set
			{
				fogType_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogType,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogType_dirty;

		/// <summary>
		/// (Sets dirty flag) Fog seed - for recall of procedural things
		/// </summary>
		public static int FogSeed
		{
			get { return fogSeed; }
			set
			{
				fogSeed = value;
				FogSeed_dirty = true;
			}
		}
		private static int fogSeed;

		public static bool FogSeed_dirty
		{
			get { return fogSeed_dirty; }
			set
			{
				fogSeed_dirty = value;
				EyeOverlaysEvent.Invoke(Instance, new EyeOverlaysEventArgs
				{
					property = EyeOverlaysEventProperty.FogSeed,
					type = value ? EyeOverlaysEventType.DIRTY : EyeOverlaysEventType.CLEARED,
				});
			}
		}
		private static bool fogSeed_dirty;

		#endregion

		//

		#region Event Methods

		/// <summary>
		/// Sends an INIT event from all observed properties - used mostly to refresh UI
		/// </summary>
		/// <param name="filter">A string filter for observers. Default is "init"</param>
		public static void BroadcastInit(string filter = "init")
		{
			EyeOverlaysEvent.InvokeAll(Instance, filter:filter);
		}

		#endregion

		//

		#region Config On Disk Public Methods

		public static void LoadConfig()
		{
			try
			{
				string json = File.ReadAllText(exeDir + @"/settings.json");
				JsonConvert.PopulateObject(json, Instance);
			}
			catch (System.Exception)
			{
				return;
			}
		}

		public static void SaveConfig()
		{
			File.WriteAllText(exeDir + @"/settings.json", JsonConvert.SerializeObject(Instance, formatting:Formatting.Indented));
		}

		#endregion
	}
}
