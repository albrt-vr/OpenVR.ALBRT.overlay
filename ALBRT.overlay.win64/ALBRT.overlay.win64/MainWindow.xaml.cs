//======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

using ALBRT.overlay.cs;
using ALBRT.overlay.cs.Data;
using ALBRT.overlay.cs.Events;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System.Linq;

namespace ALBRT.overlay.win64
{
	public sealed partial class MainWindow : Window
	{
		private AppWindow appWindow;
		private readonly SolidColorBrush backgroundWhite = new(Colors.White);
		private readonly SolidColorBrush blackForeground = new(Colors.Black);
		private readonly SolidColorBrush whiteForeground = new(Colors.White);

		// constructor

		public MainWindow()
		{
			InitializeComponent();
			GetAppWindow(); // get the parent app window so we can handle window events
			appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 2400, Height = 1200 });
			// TODO ON RELEASE comment out
			//appWindow.Move(new Windows.Graphics.PointInt32 { X = 0, Y = 0 });

			// subscribe to events
			appWindow.Closing += OnClosing;
			ALBRTManagerEvent.OnEvent += OnALBRTEvent;
			EyeOverlaysEvent.OnChange += OnEyeOverlaysChange;

			// make sure the app is hidden until we hear back from the back-end
			// TODO ON RELEASE false
			ViewApp(false);

			// manually set the title as there is no package manifest (AppInfo) for unpackaged apps. AppInfo is null
			Title = AppTitleText.Text; // utilise the titlebar text field as the master title substitute
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);

			// start the back end
			ALBRTManager.Start();

			// await an event from the manager to indicate the back end has loaded
		}

		// app lifecycle methods

		/// <summary>
		/// Init the app; now APIs are initialised and data is loaded
		/// </summary>
		private void Start()
		{
			EyeOverlays.BroadcastInit(filter:"mainwindow");
			ViewApp(true); // remove the splash and show the app
			AppTitleState.Text = "Running"; // set the title bar state text
			BackgroundNavChoiceChanged(); // reflect the mask type in the UI
		}

		/// <summary>
		/// View the app, as opposed to the splash
		/// </summary>
		private void ViewApp(bool viewApp)
		{
			Splash.Visibility = viewApp ? Visibility.Collapsed : Visibility.Visible;
			MasterView.Visibility = viewApp ? Visibility.Visible : Visibility.Collapsed;
			AppInfoBar.Visibility = viewApp ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>
		/// Shut down the app
		/// </summary>
		private void Quit()
		{
			// NOTE this is distinct from the window OnClosing event

			// unsubscribe events - this is completely redundant as this is the main view, so closing quits the app, but as this is an example app I am leaving it here
			appWindow.Closing -= OnClosing;
			EyeOverlaysEvent.OnChange -= OnEyeOverlaysChange;
			ALBRTManagerEvent.OnEvent -= OnALBRTEvent;

			// close the app
			Close();
		}

		/// <summary>
		/// The manager back-end has failed for some reason
		/// </summary>
		private void BackEndFailed(string e = "[E:]", string s = "Unknown")
		{
			// NOTE this is designed to give the user strong feedback, as the app will most often be viewed in a VR window where minor feddback is useless

			AppTitleState.Text = $"ERROR";
			SplashMessage.Text = $"Error: {e}\n\nSolution: {s}"; // print error message into splash
			AppTitleBar.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 169, 0, 0)); // title bar red tint to inform user
			ViewApp(false); // change back to the splash with the error info
		}

		// other methods

		private void GetAppWindow()
		{
			// Retrieve the window handle (HWND) of the current (XAML) WinUI 3 window.
			var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

			// Retrieve the WindowId that corresponds to hWnd.
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

			// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
			appWindow = AppWindow.GetFromWindowId(windowId);
		}

		// UI navigation event handlers

		private void OnMasterViewLoaded(object sender, RoutedEventArgs e)
		{
			NavigationViewItem selected = MasterView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag == EyeOverlays.OverlayMaskType.ToString());
			selected.IsSelected = true;
			ContentFrame.Navigate(typeof(SettingsPage), EyeOverlays.OverlayMaskType);
		}

		private void OnMasterViewInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
		{
			if (e.IsSettingsInvoked)
			{
				ContentFrame.Navigate(typeof(SettingsPage));
				return;
			}

			// find NavigationViewItem with Content that equals InvokedItem
			NavigationViewItem item = sender.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Content == (string)e.InvokedItem);

			// we found nothing in MenuItems so look in FooterMenuItems
			if (item == null) item = sender.FooterMenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)e.InvokedItem);

			switch (item.Name)
			{
				// handle pure click events - they do stuff but dont change the view

				case "Toggle_OverlaysVisible":
					EyeOverlays.EyeOverlaysVisible = !EyeOverlays.EyeOverlaysVisible;
					break;

				case "Toggle_SwitchedEyes":
					EyeOverlays.EyeOverlaysSwitched = !EyeOverlays.EyeOverlaysSwitched;
					break;

				case "Toggle_AlphaT":
					EyeOverlays.AlphaTEnabled = !EyeOverlays.AlphaTEnabled;
					break;

				case "Toggle_OverlaysVisibleInOVRDash":
					EyeOverlays.EyeOverlaysHideInDash = !EyeOverlays.EyeOverlaysHideInDash;
					break;

				// handle navigation - note the settings button is not in the xaml, it is part of the navigationview

				case "Open_PatchOverlaySettings":
					EyeOverlays.OverlayMaskType = MaskType.PATCH;
					ContentFrame.Navigate(typeof(SettingsPage), EyeOverlays.OverlayMaskType);
					BackgroundNavChoiceChanged();
					break;

				case "Open_SlatOverlaySettings":
					EyeOverlays.OverlayMaskType = MaskType.SLAT;
					ContentFrame.Navigate(typeof(SettingsPage), EyeOverlays.OverlayMaskType);
					BackgroundNavChoiceChanged();
					break;
			}
		}

		/// <summary>
		/// Handles informing the user of the background navigation - a secondary choice that affects the app rendering method - see MaskType
		/// </summary>
		private void BackgroundNavChoiceChanged()
		{

			Open_PatchOverlaySettings.Background = null;
			Open_SlatOverlaySettings.Background = null;
			//Open_FogOverlaySettings.Background = null;

			Open_PatchOverlaySettings.Foreground = whiteForeground;
			Open_SlatOverlaySettings.Foreground = whiteForeground;
			//Open_FogOverlaySettings.Foreground = whiteForeground;

			MaskType type = EyeOverlays.OverlayMaskType;

			switch (type)
			{
				case MaskType.PATCH:
					Open_PatchOverlaySettings.Background = backgroundWhite;
					Open_PatchOverlaySettings.Foreground = blackForeground;
					break;
				case MaskType.SLAT:
					Open_SlatOverlaySettings.Background = backgroundWhite;
					Open_SlatOverlaySettings.Foreground = blackForeground;
					break;
					//case MaskType.FOG:
					//	Open_FogOverlaySettings.Background = backgroundWhite;
					//	Open_FogOverlaySettings.Foreground = blackForeground;
					//	break;
			}
		}

		private void OnMasterViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
		{
		}

		// subscribed event handlers

		/// <summary>
		/// Handle the events raised by ALBRT Runtime - see the shared project ALBRT.overlay.cs
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Event</param>
		private void OnALBRTEvent(object sender, ALBRTManagerEventArgs e)
		{
			// 'console' feedback
			AppInfoConsole.Text = e.type.ToString(); // print the last event - add context below

			// NOTE in c# if you need to fall through with after code block you must call 'goto case'
			switch (e.type)
			{
				case ALBRTManagerEventType.ALBRT_LOADING:
					break;

				case ALBRTManagerEventType.ALBRT_STARTED:
					// infobar feedback
					InDashValue.Text = ALBRTManager.OpenVRIsDashboardOpen ? "open" : "closed";
					Start();
					break;

				case ALBRTManagerEventType.ALBRT_ERROR:
					BackEndFailed(e.printError.error, e.printError.solution);
					break;

				case ALBRTManagerEventType.ALBRT_QUIT:
					Quit(); // quit the app
					break;

				case ALBRTManagerEventType.VR_DASHBOARD_OPENED:
					// infobar feedback
					InDashValue.Text = "open";
					if (EyeOverlays.EyeOverlaysHideInDash) OverlaysHiddenValue.Text = "yes";
					break;

				case ALBRTManagerEventType.VR_DASHBOARD_CLOSED:
					// infobar feedback
					InDashValue.Text = "closed";
					OverlaysHiddenValue.Text = "no";
					break;
			}
		}

		/// <summary>
		/// Handle the events raised by ALBRT EyeOverlays - see the shared project ALBRT.overlay.cs
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Event</param>
		private void OnEyeOverlaysChange(object sender, EyeOverlaysEventArgs e)
		{
			// filtering
			if (e.type != EyeOverlaysEventType.CLEARED && e.type != EyeOverlaysEventType.INIT) return;
			if (e.type == EyeOverlaysEventType.INIT && e.filter != "mainwindow") return; // this init was not for us

			string changed = $"Δ {e.property}";
			string statement = ""; // fill this out if you wish to print something to the little 'console' in the bottom right for debug

			// NOTE we could report a 'console' statement to the user for every single property CLEARED event, but it is mainly for debug, so just print what seems relevant
			// the main properties that the user or a co-pilot should know about are printed in the infobar at the bottom of the app window
			// note that some of the infobar values are changed in the OnALBRTEvent method

			switch (e.property)
			{
				case EyeOverlaysEventProperty.IPD_CHANGED:
					statement = $"{changed}  •  {ALBRTManager.ReadableIPD}";

					// infobar feedback
					IPDValue.Text = ALBRTManager.ReadableIPD;
					break;

				case EyeOverlaysEventProperty.OverlayMaskType:
					statement = $"{changed}  •  {EyeOverlays.OverlayMaskType}";

					// infobar feedback
					OverlayMethodValue.Text = EyeOverlays.OverlayMaskType.ToString().ToLower();
					break;

				case EyeOverlaysEventProperty.EyeOverlaysHideInDash:
					statement = $"{changed}  •  {EyeOverlays.EyeOverlaysHideInDash}";
					OverlaysHiddenValue.Text = (EyeOverlays.EyeOverlaysHideInDash && ALBRTManager.OpenVRIsDashboardOpen && EyeOverlays.EyeOverlaysVisible) ? "yes" : "no";

					// UI feedback
					Toggle_Icon_OverlaysVisibleInOVRDash.Glyph = EyeOverlays.EyeOverlaysHideInDash ? "\uE75B" : "\uE75B";
					Toggle_OverlaysVisibleInOVRDash.Background = EyeOverlays.EyeOverlaysHideInDash ? backgroundWhite : null;
					Toggle_OverlaysVisibleInOVRDash.Foreground = EyeOverlays.EyeOverlaysHideInDash ? blackForeground : whiteForeground;
					break;

				case EyeOverlaysEventProperty.EyeOverlaysVisible:
					statement = $"{changed}  •  {EyeOverlays.EyeOverlaysVisible}";

					// infobar feedback
					OverlaysOnValue.Text = EyeOverlays.EyeOverlaysVisible ? "on": "off";

					// UI feedback
					Toggle_Icon_OverlaysVisible.Glyph = EyeOverlays.EyeOverlaysVisible ? "\uE81E" : "\uE81E";
					Toggle_OverlaysVisible.Background = EyeOverlays.EyeOverlaysVisible ? backgroundWhite : null;
					Toggle_OverlaysVisible.Foreground = EyeOverlays.EyeOverlaysVisible ? blackForeground : whiteForeground;
					break;

				case EyeOverlaysEventProperty.EyeOverlaysSwitched:
					statement = $"{changed}  •  {EyeOverlays.EyeOverlaysSwitched}";

					// infobar feedback
					EyesSwitchedValue.Text = EyeOverlays.EyeOverlaysSwitched ? "yes": "no";

					// UI feedback
					Toggle_Icon_SwitchedEyes.Glyph = EyeOverlays.EyeOverlaysSwitched ? "\uE8AB" : "\uE8AB";
					Toggle_SwitchedEyes.Background = EyeOverlays.EyeOverlaysSwitched ? backgroundWhite : null;
					Toggle_SwitchedEyes.Foreground = EyeOverlays.EyeOverlaysSwitched ? blackForeground : whiteForeground;
					break;

				case EyeOverlaysEventProperty.AlphaTEnabled:
					statement = $"{changed}  •  {EyeOverlays.AlphaTEnabled}";

					// infobar feedback
					AlphaTValue.Text = EyeOverlays.AlphaTEnabled ? "on" : "off";

					// UI feedback
					Toggle_Icon_AlphaT.Glyph = EyeOverlays.AlphaTEnabled ? "\uE916" : "\uE916";
					Toggle_AlphaT.Background = EyeOverlays.AlphaTEnabled ? backgroundWhite : null;
					Toggle_AlphaT.Foreground = EyeOverlays.AlphaTEnabled ? blackForeground : whiteForeground;
					break;

				case EyeOverlaysEventProperty.EyeToRender:
					statement = $"{changed}  •  {EyeOverlays.EyeToRender}";

					// infobar feedback
					EyeRenderingValue.Text = EyeOverlays.EyeToRender.ToString().ToLower();
					break;
			}
			if (!string.IsNullOrEmpty(statement) && e.type != EyeOverlaysEventType.INIT) AppInfoConsole.Text = statement; // print to the little 'console'
		}

		private void OnClosing(object sender, AppWindowClosingEventArgs e)
		{
			// NOTE this is distinct from the Quit event

			// block the window closure until we are done
			e.Cancel = true;

			// perform async tasks

			// perform other tasks in other places
			ALBRTManager.Quit();

			// quit the app
			Quit();
		}

	}
}