// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny
// The TODO tags being used are : TODO(stuff), CRIT(major issue/broken), BUG(needs fixing), HERE(bookmarks)
// Please try to keep all names readable in English. Do not aggressively shorten names unless accepted shorthand (ie w for width; x for x axis)
// Keep in mind this project is a tutorial exploring VR overlays, so prefer over explaining and commenting rather than brevity

using ALBRT.overlay.cs.Data;
using ALBRT.overlay.cs.Events;
using ALBRT.overlay.cs.Interfaces;

using System;
using System.Threading;

using Valve.VR;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using LearnOpenTK.Common;

namespace ALBRT.overlay.cs
{
	internal class ALBRTManager : IALBRTManagerEventSender
	{
		#region Singleton

		private ALBRTManager() { }

		//

		public static ALBRTManager Instance
		{
			get
			{
				instance ??= new ALBRTManager();
				return instance;
			}
		}
		private static ALBRTManager instance;

		#endregion

		//

		#region Frame Rendering Constant Fields

		private const int appFPS = 90; // FPS for the app process - used for polling, etc (this is the theoretical frametime within VR in this context, we defer on OpenVR for rendering)
		private const float frame = 1000 / appFPS; // to ms frame time

		#endregion

		#region Time Fields

		private static long millisecondsAtStart;
		private static long millisecondsSinceStart;
		private static float timeF; // float time in seconds
		private static PeriodicTimer t = null;

		#endregion

		#region OpenVR Fields

		private static VREvent_t openVRpolledEvent = new();

		#endregion

		#region System Disk Fields

		private static readonly string exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

		#endregion

		#region Error String Fields

		private static readonly string errorNoHMD = "SteamVR not running / No HMD.  [E:0]";
		private static readonly string solutionNoHMD = "Ensure SteamVR is working with an HMD connected, then restart this app.";
		private static readonly string errorOverlayAppFailed = "OpenVR overlay app failed / No HMD.  [E:1]";
		private static readonly string solutionOverlayAppFailed = "Ensure an HMD is connected and restart SteamVR, then restart this app.";
		private static readonly string errorOverlayRenderFailed = "Overlays error / Running twice.  [E:2]";
		private static readonly string solutionOverlayRenderFailed = "Close this instance of the app. / Restart SteamVR then, restart this app.";

		#endregion

		#region Manager State Fields

		private static bool startedManager = false;
		private static bool startedGL = false;
		private static bool openVROverlaysCreated = false;
		private static bool isPolling = false;

		#endregion

		#region OpenVR Live Transform Fields

		private static HmdMatrix34_t leftTransform = new(); // runtime - m3 == 'IPD'  m11 == 'Distance' (- is forward)
		private static HmdMatrix34_t rightTransform = new(); // runtime - m3 == 'IPD'  m11 == 'Distance' (- is forward)

		#endregion

		#region OpenVR Overlay Handle Fields

		public static ulong left;
		public static ulong right;

		#endregion

		#region Rendering Fields

		private const int textureWidth = 4096; // stereo, so double wide
		private const int textureHeight = 2048;

		private static unsafe Window* GLWindow;
		private static int frameBufferID;
		private static int renderBufferID;
		private static int dummyVertexArrayID;

		private static Texture_t leftTexture; // allows us to pass the RT to SteamVR
		private static Texture_t rightTexture;

		private static int leftTextureID;
		private static int rightTextureID;

		private static Shader overlaysShader2D;
		//private static Shader overlaysShader3D;

		private static bool needsGLRender; // this frame needs a fresh GL render

		#endregion

		#region Misc Fields

		private readonly Random Rng = new();

		#endregion

		//

		#region Public Properties

		/// <summary>
		/// Is the SteamVR dashboard showing?
		/// </summary>
		public static bool OpenVRIsDashboardOpen { get; private set; }

		/// <summary>
		/// The IPD in mm as a string for display
		/// </summary>
		public static string ReadableIPD
		{
			get
			{
				return (EyeOverlays.x * 2f * 1000f).ToString("F2") + "mm";
			}
		}

		/// <summary>
		/// The overlay depth in mm as a string for display (abs)
		/// </summary>
		public static string ReadableOverlayDepth
		{
			get
			{
				return Math.Abs(EyeOverlays.z * 1000f).ToString("F2") + "mm";
			}
		}

		/// <summary>
		/// The overlay quad dimension in mm as a string for display
		/// </summary>
		public static string ReadableOverlayQuadWidth
		{
			get
			{
				return (EyeOverlays.w * 1000f).ToString("F2") + "mm";
			}
		}

		#endregion

		//

		#region Manager Lifecycle Methods

		public static void Start()
		{
			if (startedManager) return;

			EyeOverlays.LoadConfig();

			// start time stamp
			millisecondsAtStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			// subscribe
			EyeOverlaysEvent.OnChange += OnEyeOverlaysDirty;

			// report starting
			ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
			{
				type = ALBRTManagerEventType.ALBRT_LOADING
			});

			// check steamvr server state
			if (!OpenVRHMDIsPresent()) return;

			// init the OpenVR app
			if (!OpenVRInitOverlayApp()) return;

			// get starting system values that could affect our rendering
			OpenVRIsDashboardOpen = OpenVR.Overlay.IsDashboardVisible();

			// create eye overlays
			if (!EyeOverlayCreate(ref left, EyeOverlays.leftKey, EyeOverlays.leftName, leftTransform, Eye.LEFT)) return;
			if (!EyeOverlayCreate(ref right, EyeOverlays.rightKey, EyeOverlays.rightName, rightTransform, Eye.RIGHT)) return;
			openVROverlaysCreated = true;

			// check eye transforms - this will set the transforms as the initial IPD cannot be X default so the transform will show dirty on init
			OpenVRCheckEyeTransforms();

			// set eye overlays visibility state
			EyeOverlaysShowHide();

			// start OpenGL
			GLStart();

			// report everything has started
			ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
			{
				type = ALBRTManagerEventType.ALBRT_STARTED
			});

			// start polling
			StartTicking();

			// unlock
			startedManager = true;
			
			// initial render of overlays
			GLOverlaysUpdate();
		}

		public static void Stop()
		{
			if (!startedManager) return;

			// unsubscribe
			EyeOverlaysEvent.OnChange -= OnEyeOverlaysDirty;

			// lock
			isPolling = false;
			startedManager = false;

			// destroy opentk window
			GLDestroy();
			
			// destroy OpenVR overlays
			OverlayDestroy(ref left);
			OverlayDestroy(ref right);

			// lock
			startedGL = false;
			openVROverlaysCreated = false;

			// stop ticking
			StopTicking();
		}

		public static void Quit()
		{
			if (!startedManager) return;

			Stop();

			// fire off an event in case we need other stuff to know

			ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
			{
				type = ALBRTManagerEventType.ALBRT_QUIT
			});

			EyeOverlays.SaveConfig();

			// becasue we currently have no other logic to handle quitting we can just shut down OpenVR now

			OpenVR.Shutdown(); // you must call this eventually or steamvr will wait forever for us to close our app
		}

		#endregion

		//

		#region Runtime Methods

		private static async void StartTicking()
		{
			if (t != null) return;

			isPolling = true;

			// this is our pseudo frame timer as we have no renderer to tie in to - it will tick roughly every 'frame' ms, which is one second (1000ms) / appFPS (frames per second)
			t = new(TimeSpan.FromMilliseconds(frame));

			while (await t.WaitForNextTickAsync()) // await the next period - continue ticking until disposed of
			{
				// track fixed time
				millisecondsSinceStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - millisecondsAtStart;
				timeF = millisecondsSinceStart / 1000f; // time as a float - this is now equivalent to a game engine fixed time float (decimalised seconds)

				Tick(); // everything else in tick
			}
		}

		private static void StopTicking()
		{
			if (t == null) return;

			t.Dispose();
		}

		private static void Tick()
		{
			if (!isPolling) return;

			isPolling = OpenVRPollEvents(); // the OpenVR events can include API quitting, so we need to track a master state for polling
			if (!isPolling) return; // polling just ended, abort

			// check the eye transforms
			OpenVRCheckEyeTransforms();

			// render
			DoEyeOverlaysRender();

			// animate alpha over time
			EyeOverlaysUpdateAlphaT();
		}

		#endregion
		
		//

		#region Event Handler Methods

		/// <summary>
		/// Handle changes in EyeOverlays
		/// </summary>
		private static void OnEyeOverlaysDirty(object? sender, EyeOverlaysEventArgs e)
		{
			if (e.type != EyeOverlaysEventType.DIRTY) return; // we don't handle cleared events here, this is a dirty shop for dirty people

			switch (e.property)
			{
				// flags

				case EyeOverlaysEventProperty.IPD_CHANGED:
					EyeOverlayUpdateTransform(left, Eye.LEFT);
					EyeOverlayUpdateTransform(right, Eye.RIGHT);
					EyeOverlays.IPD_CHANGED = false;
					break;

				// properties

				case EyeOverlaysEventProperty.VirtualDistance:
					// TODO the fog of war stuff has been removed from the public repo
					EyeOverlays.VirtualDistance_dirty = false;
					break;

				case EyeOverlaysEventProperty.Alpha:
					EyeOverlayUpdateAlpha(left);
					EyeOverlayUpdateAlpha(right);
					EyeOverlays.Alpha_dirty = false;
					break;

				case EyeOverlaysEventProperty.OverlayMaskType:
					needsGLRender = true;
					EyeOverlays.OverlayMaskType_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogColour:
					needsGLRender = true;
					EyeOverlays.FogColour_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogAnimated:
					needsGLRender = true;
					EyeOverlays.FogAnimated_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogSpeed:
					needsGLRender = true;
					EyeOverlays.FogSpeed_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogDirection:
					needsGLRender = true;
					EyeOverlays.FogDirection_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogType:
					needsGLRender = true;
					EyeOverlays.FogType_dirty = false;
					break;

				case EyeOverlaysEventProperty.FogSeed:
					needsGLRender = true;
					EyeOverlays.FogSeed_dirty = false;
					break;

				case EyeOverlaysEventProperty.PatchColour:
					needsGLRender = true;
					EyeOverlays.PatchColour_dirty = false;
					break;

				case EyeOverlaysEventProperty.PatchType:
					needsGLRender = true;
					EyeOverlays.PatchType_dirty = false;
					break;

				case EyeOverlaysEventProperty.PatchRadialSize:
					needsGLRender = true;
					EyeOverlays.PatchRadialSize_dirty = false;
					break;

				case EyeOverlaysEventProperty.PatchRadialSoftness:
					needsGLRender = true;
					EyeOverlays.PatchRadialSoftness_dirty = false;
					break;

				case EyeOverlaysEventProperty.SlatColour:
					needsGLRender = true;
					EyeOverlays.SlatColour_dirty = false;
					break;

				case EyeOverlaysEventProperty.SlatHeight:
					needsGLRender = true;
					EyeOverlays.SlatHeight_dirty = false;
					break;

				case EyeOverlaysEventProperty.SlatSliceHeight:
					needsGLRender = true;
					EyeOverlays.SlatSliceHeight_dirty = false;
					break;

				case EyeOverlaysEventProperty.SlatSliceOffset:
					needsGLRender = true;
					EyeOverlays.SlatSliceOffset_dirty = false;
					break;

				case EyeOverlaysEventProperty.EyeOverlaysHideInDash:
					if (OpenVRIsDashboardOpen) EyeOverlaysShowHide();
					EyeOverlays.EyeOverlaysHideInDash_dirty = false;
					break;

				case EyeOverlaysEventProperty.EyeOverlaysVisible:
					EyeOverlaysShowHide();
					EyeOverlays.EyeOverlaysVisible_dirty = false;
					break;

				case EyeOverlaysEventProperty.EyeToRender:
					needsGLRender = true;
					EyeOverlays.EyeToRender_dirty = false;
					break;

				case EyeOverlaysEventProperty.EyeOverlaysSwitched:
					SetEyeOverlaysTextures();
					// TODO in the fog of war projections is there symmetry in the textures allowing this?
					EyeOverlays.EyeOverlaysSwitched_dirty = false;
					break;

				case EyeOverlaysEventProperty.AlphaTEnabled:
					if (!EyeOverlays.AlphaTEnabled) // we exited alpha T mode - go back to master alpha
					{
						EyeOverlayUpdateAlpha(left);
						EyeOverlayUpdateAlpha(right);
					}
					EyeOverlays.AlphaTEnabled_dirty = false;
					break;

				case EyeOverlaysEventProperty.AlphaTSpeed:
					// if alpha T is on this will just be read during update
					EyeOverlays.AlphaTSpeed_dirty = false;
					break;

				case EyeOverlaysEventProperty.AlphaTType:
					// if alpha T is on this will just be read during update
					EyeOverlays.AlphaTType_dirty = false;
					break;
			}
		}

		#endregion

		//

		#region OpenGL Methods

		/// <summary>
		/// Set up everything we need for GL rendering of both eye textures
		/// </summary>
		private static unsafe void GLStart()
		{
			if (startedGL) return;

			GLFW.Init();

			// TODO this needs to be a general purpose implementation - keep in mind we are rendering full viewport FBOs AND we plan actual 3D scene rendering (fog of war masking)
			// with that in mind, do not strip out things just because we don't need them yet

			// window settings
			// TODO ON RELEASE false
			bool showWindow = false;
			GLFW.WindowHint(WindowHintBool.Visible, showWindow);
			GLFW.WindowHint(WindowHintBool.Floating, showWindow);
			GLFW.WindowHint(WindowHintBool.Decorated, showWindow);
			GLFW.WindowHint(WindowHintBool.TransparentFramebuffer, showWindow);

			// window creation
			GLWindow = GLFW.CreateWindow(textureWidth, textureHeight, "ALBRT Overlay Renderer", null, null);
			// TODO ON RELEASE comment out
			//GLFW.SetWindowPos(GLWindow, 100, 100);

			// context
			GLFW.MakeContextCurrent(GLWindow);
			GL.LoadBindings(new GLFWBindingsContext());

			// blending
			// TODO needs review - don't have time
			GL.Enable(EnableCap.Blend);
			GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);

			// compile shaders
			overlaysShader2D = new(exeDir + "/Shaders/Overlays2D.vert", exeDir + "/Shaders/Overlays2D.frag");
			//overlaysShader3D = new(exeDir + "/Shaders/Overlays3D.vert", exeDir + "/Shaders/Overlays3D.frag"); // removed for now

			// generate texture ID
			leftTextureID = GL.GenTexture();
			// set up
			setupTexture(leftTextureID);

			// generate texture ID
			rightTextureID = GL.GenTexture();
			// set up
			setupTexture(rightTextureID);

			static void setupTexture(int id)
			{
				// set texture binding
				GL.BindTexture(TextureTarget.Texture2D, id);
				// filtering
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
				// wrapping
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // U
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge); // V
				// define the texture in memory
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureWidth, textureHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
				
				// unbind
				GL.BindTexture(TextureTarget.Texture2D, 0);
			}

			// set up the frame buffer
			frameBufferID = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferID);
			GL.Viewport(0, 0, textureWidth, textureHeight);
			GL.ClearColor(1f, 0f, 1f, 1f); // clear to magenta so we can see if there are init issues

			// set up the depth buffer
			// TODO we will not need a depth buffer for the 2D renders, but I do not know if the FBO needs one to pass complete - will check later
			renderBufferID = GL.GenRenderbuffer();
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBufferID);
			GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, textureWidth, textureHeight);

			// attach render buffer as depth
			GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.Depth, RenderbufferTarget.Renderbuffer, renderBufferID);

			// attach textures as colour attachments 0 and 1
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, leftTextureID, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, rightTextureID, 0);

			// set drawbuffers
			DrawBuffersEnum[] DrawBuffers = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
			GL.DrawBuffers(2, DrawBuffers);

			// check the framebuffer
			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) throw new System.Exception("init buffer failed");

			// clear
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// switch back to window/screen frame buffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			// set up junk vertex array to force rendering
			dummyVertexArrayID = GL.GenVertexArray();

			// setup texture data for OpenVR
			leftTexture = new Texture_t
			{
				handle = (IntPtr)leftTextureID,
				eType = ETextureType.OpenGL,
				eColorSpace = EColorSpace.Linear,
			};

			rightTexture = new Texture_t
			{
				handle = (IntPtr)rightTextureID,
				eType = ETextureType.OpenGL,
				eColorSpace = EColorSpace.Linear,
			};

			startedGL = true;
		}

		/// <summary>
		/// Destroy everything we need for GL rendering
		/// </summary>
		private static unsafe void GLDestroy()
		{
			GLFW.DestroyWindow(GLWindow);
		}

		/// <summary>
		/// (UPDATE BOTH) Renders both eye overlay textures
		/// </summary>
		private static unsafe void GLOverlaysUpdate()
		{
			if (!startedGL || !openVROverlaysCreated) return;

			// set rendering destination to our offscreen buffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferID);

			// clear
			GL.ClearColor(0f, 0f, 0f, 0f); // transparent black
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// set viewport to texture size
			GL.Viewport(0, 0, textureWidth, textureHeight);

			// set shader program
			switch (EyeOverlays.OverlayMaskType)
			{
				case MaskType.SLAT:
				case MaskType.PATCH:
					overlaysShader2D.Use(); // !USE FIRST
					UpdateOverlays2DShader();
					break;

				case MaskType.FOG:
					//overlaysShader3D.Use(); // !USE FIRST
					// removed for now
					break;
			}

			// force scene render - I am assuming here that we need a VBO so the render is not optimised away?
			GL.BindVertexArray(dummyVertexArrayID);
			// just call a draw that does nothing
			GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

			// switch back to window/screen buffer && swap double buffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GLFW.SwapBuffers(GLWindow);

			// push textures
			SetEyeOverlaysTextures();
		}

		/// <summary>
		/// Update ALL uniforms for overlays2D Shader
		/// </summary>
		private static void UpdateOverlays2DShader()
		{
			// set uniform values

			int timeLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "time");
			GL.Uniform1(timeLocation, timeF);

			int maskTypeLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "maskType");
			GL.Uniform1(maskTypeLocation, (int)EyeOverlays.OverlayMaskType);

			int eyeLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "eye");
			GL.Uniform1(eyeLocation, (int)EyeOverlays.EyeToRender);

			// slats

			int slatColourLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "slatColour");
			GL.Uniform3(slatColourLocation, new OpenTK.Mathematics.Vector3(EyeOverlays.SlatColour.X, EyeOverlays.SlatColour.Y, EyeOverlays.SlatColour.Z));

			int slatHeightLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "slatHeight");
			GL.Uniform1(slatHeightLocation, EyeOverlays.SlatHeight);

			int slatGapHeightLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "sliceHeight");
			GL.Uniform1(slatGapHeightLocation, EyeOverlays.SlatSliceHeight);

			int slatOffsetLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "sliceOffset");
			GL.Uniform1(slatOffsetLocation, EyeOverlays.SlatSliceOffset);

			// patch

			int patchColourLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "patchColour");
			GL.Uniform3(patchColourLocation, new OpenTK.Mathematics.Vector3(EyeOverlays.PatchColour.X, EyeOverlays.PatchColour.Y, EyeOverlays.PatchColour.Z));

			int patchTypeLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "patchType");
			GL.Uniform1(patchTypeLocation, (int)EyeOverlays.PatchType);

			int patchRadialSizeLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "patchRadialSize");
			GL.Uniform1(patchRadialSizeLocation, EyeOverlays.PatchRadialSize);

			int patchRadialSoftnessLocation = GL.GetUniformLocation(overlaysShader2D.Handle, "patchRadialSoftness");
			GL.Uniform1(patchRadialSoftnessLocation, EyeOverlays.PatchRadialSoftness);
		}

		/// <summary>
		/// Update ALL uniforms for overlays3D Shader
		/// </summary>
		private static void UpdateOverlays3DShader()
		{
			// removed for now
		}

		/// <summary>
		/// (UPDATE BOTH) Render the overlay textures using OpenGL
		/// </summary>
		private static void DoEyeOverlaysRender()
		{
			if (!startedGL || !openVROverlaysCreated) return;

			// TODO? this is not frame safe, this could be resolved on event frame or event frame +1
			// do a fresh render if needed
			if (needsGLRender) GLOverlaysUpdate();
			needsGLRender = false;

			// we only have a single type that will render every frame (fog of war) becasue it is animated, the rest should set needsGLRender = true on state change events
			// and because OpenVR has an overlay alpha control we will handle alpha animation in OpenVR not OpenGL

			if (EyeOverlays.OverlayMaskType != MaskType.FOG) return;

			// removed for now

			// NOTE we are treating the eye overlays as camera frustrums now, and rendering our own OpenGL scene into them as render textures
			// this means we effectively have a layered camera on top of the SteamVR dashboard camera, into which we can render anything in 3D
		}

		#endregion

		//

		#region OpenVR Overlay Update Methods

		/// <summary>
		/// (UPDATE BOTH) Alpha T as a full quad alpha aninmation
		/// </summary>
		private static void EyeOverlaysUpdateAlphaT()
		{
			if (!openVROverlaysCreated) return;

			if (EyeOverlays.AlphaTEnabled) // applies to all mask types
			{
				float o = 1f;
				switch (EyeOverlays.AlphaTType)
				{
					case AnimationType.A01:
						o = (timeF % EyeOverlays.AlphaTSpeed) / EyeOverlays.AlphaTSpeed;
						break;

					case AnimationType.A10:
						o = 1 - ((timeF % EyeOverlays.AlphaTSpeed) / EyeOverlays.AlphaTSpeed);
						break;

					case AnimationType.APINGPONG:
						o = (MathF.Sin(timeF / (EyeOverlays.AlphaTSpeed / 6.0f)) + 1.0f) / 2.0f;
						break;

					default:
						break;
				}
				OpenVR.Overlay.SetOverlayAlpha(left, o);
				OpenVR.Overlay.SetOverlayAlpha(right, o);
				// IMPORTANT we will handle the exit of alpha t in an cleared event, re setting it to A, do not do stuff like that in tick
			}
		}

		/// <summary>
		/// (UPDATE ONE) Updates the transform of the eye overlay
		/// </summary>
		private static void EyeOverlayUpdateTransform(ulong handle, Eye eye)
		{
			if (!openVROverlaysCreated) return;

			EyeOverlays.x = rightTransform.m3; // cache the new HMD IPD(positive half) as we are just about to resolve the delta

			// TODO conduct a test to see if the divergence here is true - just because ti feels intuitively correct doesnt mean it is
			float correctedIPDHalf = EyeOverlays.x * (EyeOverlays.z * -1); // the diverged corrected IPD for overlay centres to appear at ~0 not at the clipping plane (101mm)
			correctedIPDHalf = EyeOverlays.x - (correctedIPDHalf / 2f); // /2 to get a single sided shift

			HmdMatrix34_t transform = rightTransform; // we take the positively positioned (right) GetEyeToHeadTransform as a base
			transform.m11 = EyeOverlays.z; // set distance
			transform.m3 = (eye == Eye.RIGHT) ? correctedIPDHalf : -correctedIPDHalf; // set IPD - right is positive from centre (nose), left is negative from centre (nose)

			OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(handle, 0, ref transform);
		}

		/// <summary>
		/// (UPDATE BOTH) Updates the alpha of the eye overlay inside OpenVR
		/// </summary>
		private static void EyeOverlayUpdateAlpha(ulong handle)
		{
			if (!openVROverlaysCreated || EyeOverlays.AlphaTEnabled) return; // skip if using alphaT
			OpenVR.Overlay.SetOverlayAlpha(handle, EyeOverlays.Alpha);
		}

		#endregion

		#region OpenVR Set Overlay Textures Methods

		/// <summary>
		/// (UPDATE BOTH) Sets the overlay textures - considers the switched eyes property
		/// </summary>
		private static void SetEyeOverlaysTextures()
		{
			if (!startedGL || !openVROverlaysCreated) return;

			// switch eye textures if needed
			Texture_t l = EyeOverlays.EyeOverlaysSwitched ? rightTexture : leftTexture;
			Texture_t r = EyeOverlays.EyeOverlaysSwitched ? leftTexture : rightTexture;

			// push textures
			OpenVR.Overlay.SetOverlayTexture(left, ref l);
			OpenVR.Overlay.SetOverlayTexture(right, ref r);
		}

		#endregion

		#region OpenVR Overlay Hiding & Showing Methods

		/// <summary>
		/// (UPDATE BOTH) Shows or hides the eye overlays depending on app state
		/// </summary>
		private static void EyeOverlaysShowHide()
		{
			if (!openVROverlaysCreated) return;

			if (EyeOverlays.EyeOverlaysVisible)
			{
				OverlayShow(left);
				OverlayShow(right);
			}
			else
			{
				OverlayHide(left);
				OverlayHide(right);
			}
		}

		/// <summary>
		/// (UPDATE ONE) Hide an overlay
		/// </summary>
		private static void OverlayHide(ulong handle)
		{
			if (!openVROverlaysCreated) return;

			if (handle == 0) return;

            OpenVR.Overlay.HideOverlay(handle);
		}

		/// <summary>
		/// (UPDATE ONE) Show an overlay
		/// </summary>
		private static void OverlayShow(ulong handle)
		{
			if (!openVROverlaysCreated) return;

			if (handle == 0) return;

			if (OpenVRIsDashboardOpen && EyeOverlays.EyeOverlaysHideInDash)
			{
				OpenVR.Overlay.HideOverlay(handle);
				return;
			}

			OpenVR.Overlay.ShowOverlay(handle);
		}

		#endregion

		#region OpenVR Overlay Creation & Destruction Methods

		/// <summary>
		/// Create an eye overlay
		/// </summary>
		private static bool EyeOverlayCreate(ref ulong handle, string key, string name, HmdMatrix34_t transform, Eye eye)
		{
			if (handle == 0) handle = (ulong)Instance.Rng.NextInt64(); // generate a new random handle

			EVROverlayError e = OpenVR.Overlay.CreateOverlay(key, name, ref handle);
			if (e != EVROverlayError.None)
			{
				handle = 0; // wipe the handle
				ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
				{
					type = ALBRTManagerEventType.ALBRT_ERROR,
					printError =
					{
						error = $"{errorOverlayRenderFailed} {key} (EVROverlayError {e})",
						solution = solutionOverlayRenderFailed
					}
				});
				return false;
			}

			OpenVR.Overlay.SetOverlayFlag(handle, (eye == Eye.RIGHT) ? VROverlayFlags.SideBySide_Crossed : VROverlayFlags.SideBySide_Parallel, true);
			OpenVR.Overlay.SetOverlayFlag(handle, VROverlayFlags.IsPremultiplied, true);
			OpenVR.Overlay.SetOverlayWidthInMeters(handle, EyeOverlays.w);
			OpenVR.Overlay.SetOverlayFromFile(handle, (eye == Eye.RIGHT) ? exeDir + $"/ImageMasks/loading.png" : exeDir + $"/ImageMasks/loading.png");
			// the transform relative to HMD will be set in the eye transform checks/set

			return true;
		}

		/// <summary>
		/// Destroy an overlay
		/// </summary>
		private static void OverlayDestroy(ref ulong handle)
		{
			if (!openVROverlaysCreated) return;

			OpenVR.Overlay.DestroyOverlay(handle);
			handle = 0;
		}

		#endregion

		#region OpenVR Live Polling Methods

		/// <summary>
		/// Poll OpenVR events
		/// </summary>
		/// <returns>False when the event found means we should stop all polling, ie the quit event</returns>
		private static bool OpenVRPollEvents()
		{
			if (OpenVR.System.PollNextEvent(ref openVRpolledEvent, (uint)System.Runtime.InteropServices.Marshal.SizeOf(openVRpolledEvent)))
			{
				switch (openVRpolledEvent.eventType)
				{
					case (int)EVREventType.VREvent_DashboardActivated:
						OpenVRIsDashboardOpen = true;

						ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
						{
							type = ALBRTManagerEventType.VR_DASHBOARD_OPENED
						});

						EyeOverlaysShowHide(); // we need to apply as they could need to be hidden
						break;

					case (int)EVREventType.VREvent_DashboardDeactivated:
						OpenVRIsDashboardOpen = false;

						ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
						{
							type = ALBRTManagerEventType.VR_DASHBOARD_CLOSED
						});

						EyeOverlaysShowHide();
						break;

					case (int)EVREventType.VREvent_Quit: // OVR is quitting now
						OpenVR.System.AcknowledgeQuit_Exiting(); // tell OpenVR to wait for us
						Quit(); // quit everything on our end
						return false; // halt polling
				}
			}
			return true;
		}

		/// <summary>
		/// Check OpenVR eye transforms for changes
		/// </summary>
		private static void OpenVRCheckEyeTransforms()
		{
			// NOTE will not change if the HMD is asleep, so the UI will not reflect things like IPD changes until HMD is awake
			// NOTE thi is polled every frame but the overlay transforms are only set when required. We just keep them up to date to observe change

			leftTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
			rightTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

			if (rightTransform.m3 != EyeOverlays.x) EyeOverlays.IPD_CHANGED = true; // flag the event
		}

		#endregion

		#region OpenVR Server State Check Methods

		/// <summary>
		/// Check that SteamVR is running. We must ensure this overlay app runs only when SteamVR has been launched correctly with an attached HMD
		/// </summary>
		private static bool OpenVRHMDIsPresent()
		{
			// this no longer always functions as expected, but can still be used to detect steamvr/hmd, just more vaguely
			// see https://steamcommunity.com/app/358720/discussions/0/133260492053018795/

			if (!OpenVR.IsHmdPresent())
			{
				ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
				{
					type = ALBRTManagerEventType.ALBRT_ERROR,
					printError =
					{
						error = errorNoHMD,
						solution = solutionNoHMD
					}
				});
				return false;
			}
			return true;
		}

		#endregion

		#region OpenVR App Initialisation Methods

		/// <summary>
		/// Init an app with the OpenVR server
		/// </summary>
		private static bool OpenVRInitOverlayApp()
		{
			EVRInitError e = EVRInitError.None;
			OpenVR.Init(ref e, EVRApplicationType.VRApplication_Overlay);
			// NOTE you could use VRApplication_Background here and check for VRInitError_Init_NoServerForBackgroundApp so the app can run without steamVR server

			if (e != EVRInitError.None)
			{
				ALBRTManagerEvent.Invoke(Instance, new ALBRTManagerEventArgs()
				{
					type = ALBRTManagerEventType.ALBRT_ERROR,
					printError =
					{
						error = $"{errorOverlayAppFailed}  (EVRInitError {e})",
						solution = solutionOverlayAppFailed
					}
				});
				return false;
			}
			return true;
		}

		#endregion
	}
}
