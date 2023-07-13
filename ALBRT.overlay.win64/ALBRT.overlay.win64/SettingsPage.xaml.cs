//======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

using ALBRT.overlay.cs.Data;
using ALBRT.overlay.cs.Events;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace ALBRT.overlay.win64
{
	// HERE I have noted that the eye rendering vs eye switching concept is very confusing for users, so it needs work.

    public sealed partial class SettingsPage : Page
    {
		private bool init = false; // lock

        public SettingsPage()
        {
			this.InitializeComponent();
		}

		// UI navigation handlers

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			// unsubscribe
			EyeOverlaysEvent.OnChange -= OnEyeOverlaysChange;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// subscribe
			EyeOverlaysEvent.OnChange += OnEyeOverlaysChange;

			// init control values
			EyeOverlays.BroadcastInit(filter:"settings");
			init = true; // unlock

			// handle entry point based on the current mask type
			switch (e.Parameter)
			{
				case MaskType.PATCH:
					// decide which settings sections to show for this activation
					FilterInfo.Text = "Showing: Patch Settings"; // inform the user that we are filtering the view
					UniversalSettings.Visibility = Visibility.Visible;
					PatchSettings.Visibility = Visibility.Visible;
					AllSettingsNote.Visibility = Visibility.Visible; // show the note about settings being filtered
					break;
				case MaskType.SLAT:
					// decide which settings sections to show for this activation
					FilterInfo.Text = "Showing: Slat Settings"; // inform the user that we are filtering the view
					UniversalSettings.Visibility = Visibility.Visible;
					SlatSettings.Visibility = Visibility.Visible;
					AllSettingsNote.Visibility = Visibility.Visible; // show the note about settings being filtered
					break;
				case MaskType.FOG:
					// decide which settings sections to show for this activation
					FilterInfo.Text = "Showing: Fog Settings"; // inform the user that we are filtering the view
					UniversalSettings.Visibility = Visibility.Visible;
					FogSettings.Visibility = Visibility.Visible;
					AllSettingsNote.Visibility = Visibility.Visible; // show the note about settings being filtered
					break;
				default:
					// you must show all sections as a default, as user has entered via the settings button
					UniversalSettings.Visibility = Visibility.Visible;
					PatchSettings.Visibility = Visibility.Visible;
					SlatSettings.Visibility = Visibility.Visible;
					FogSettings.Visibility = Visibility.Visible;
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
			if (e.type == EyeOverlaysEventType.INIT && e.filter != "settings") return; // this init was not for us

			switch (e.property)
			{
				case EyeOverlaysEventProperty.Alpha:
					// NOTE we are not concerned with float -> double casting accuracy in this app
					AlphaSlider.Value = (double)EyeOverlays.Alpha;
					break;

				case EyeOverlaysEventProperty.PatchColour:
					Windows.UI.Color pc = new()
					{
						R = (byte)(EyeOverlays.PatchColour.X * 255), // float01 (0-1) value to byte(0-255) value
						G = (byte)(EyeOverlays.PatchColour.Y * 255),
						B = (byte)(EyeOverlays.PatchColour.Z * 255),
						A = 1 * 255,
					};
					PatchColour.Color = pc;
					break;
				case EyeOverlaysEventProperty.PatchType:
					PatchTypeRadioButtons.SelectedIndex = (int)EyeOverlays.PatchType - 1;
					break;
				case EyeOverlaysEventProperty.PatchRadialSize:
					PatchRadialSizeSlider.Value = (double)EyeOverlays.PatchRadialSize;
					break;
				case EyeOverlaysEventProperty.PatchRadialSoftness:
					PatchRadialSoftnessSlider.Value = (double)EyeOverlays.PatchRadialSoftness;
					break;

				case EyeOverlaysEventProperty.SlatColour:
					Windows.UI.Color sc = new()
					{
						R = (byte)(EyeOverlays.SlatColour.X * 255),
						G = (byte)(EyeOverlays.SlatColour.Y * 255),
						B = (byte)(EyeOverlays.SlatColour.Z * 255),
						A = 1 * 255,
					};
					SlatColour.Color = sc;
					break;
				case EyeOverlaysEventProperty.SlatHeight:
					SlatHeightSlider.Value = (double)EyeOverlays.SlatHeight;
					break;
				case EyeOverlaysEventProperty.SlatSliceHeight:
					SlatSliceHeightSlider.Value = (double)EyeOverlays.SlatSliceHeight;
					break;
				case EyeOverlaysEventProperty.SlatSliceOffset:
					SlatOffsetSlider.Value = (double)EyeOverlays.SlatSliceOffset;
					break;


				case EyeOverlaysEventProperty.EyeOverlaysHideInDash:
					HideInDash.IsChecked = EyeOverlays.EyeOverlaysHideInDash;
					break;
				case EyeOverlaysEventProperty.EyeOverlaysVisible:
					OverlaysOn.IsChecked = EyeOverlays.EyeOverlaysVisible;
					break;
				case EyeOverlaysEventProperty.EyeToRender:
					EyeRadioButtons.SelectedIndex = (int)EyeOverlays.EyeToRender - 1;
					break;
				case EyeOverlaysEventProperty.EyeOverlaysSwitched:
					SwitchEyes.IsChecked = EyeOverlays.EyeOverlaysSwitched;
					break;


				case EyeOverlaysEventProperty.AlphaTEnabled:
					AlphaT.IsChecked = EyeOverlays.AlphaTEnabled;
					break;
				case EyeOverlaysEventProperty.AlphaTSpeed:
					AlphaTSpeedSlider.Value = (double)EyeOverlays.AlphaTSpeed;
					break;
				case EyeOverlaysEventProperty.AlphaTType:
					AlphaTTypeRadioButtons.SelectedIndex = (int)EyeOverlays.AlphaTType - 1;
					break;


				case EyeOverlaysEventProperty.FogColour:
					// TODO
					break;
				case EyeOverlaysEventProperty.FogAnimated:
					// TODO
					break;
				case EyeOverlaysEventProperty.FogSpeed:
					// TODO
					break;
				case EyeOverlaysEventProperty.FogDirection:
					// TODO
					break;
				case EyeOverlaysEventProperty.FogType:
					// TODO
					break;
				case EyeOverlaysEventProperty.FogSeed:
					// TODO
					break;
				default:
					break;
			}
		}

		// UI event handlers

		private void OnInfo(object sender, RoutedEventArgs e)
		{
			IEnumerable<StackPanel> settingsPanels = Settings.Children.OfType<StackPanel>();
			foreach (StackPanel panel in settingsPanels)
			{
				IEnumerable<TextBlock> textBlocks = panel.Children.OfType<TextBlock>();
				foreach (TextBlock textBlock in textBlocks)
				{
					if (textBlock.Tag == null) continue;
					if (textBlock.Tag.ToString() == "INFO") textBlock.Visibility = textBlock.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
				}
			}
		}

		private void OnAlphaSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.Alpha = (float)AlphaSlider.Value;
		}

		private void OnEyeRadioButtons_SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.EyeToRender = (Eye)EyeRadioButtons.SelectedIndex + 1;
		}

		private void OnAlphaTSpeedSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			// TODO here is a good example of a place we should only fire when the user is done interacting with the control to prevent chaotic changes
			// however the thumb is not configurable with pointer events unless we get the control at runtime, so it needs work
			if (!init) return;
			EyeOverlays.AlphaTSpeed = (float)AlphaTSpeedSlider.Value;
		}

		private void OnAlphaTTypeRadioButtons_SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.AlphaTType = (AnimationType)AlphaTTypeRadioButtons.SelectedIndex + 1;
		}

		private void OnOverlaysOn(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.EyeOverlaysVisible = (bool)OverlaysOn.IsChecked; // must cast null to false 
		}

		private void OnSwitchEyes(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.EyeOverlaysSwitched = (bool)SwitchEyes.IsChecked; // must cast null to false 
		}

		private void OnAlphaT(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.AlphaTEnabled = (bool)AlphaT.IsChecked; // must cast null to false 
		}

		private void OnHideInDash(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.EyeOverlaysHideInDash = (bool)HideInDash.IsChecked; // must cast null to false 
		}

		private void OnPatchColour_ColorChanged(object sender, ColorChangedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.PatchColour = new System.Numerics.Vector3(PatchColour.Color.R / 255f, PatchColour.Color.G / 255f, PatchColour.Color.B / 255f); // byte to float value
		}

		private void OnPatchTypeRadioButtons_SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.PatchType = (PatchType)PatchTypeRadioButtons.SelectedIndex + 1;
		}

		private void OnPatchRadialSizeSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.PatchRadialSize = (float)PatchRadialSizeSlider.Value;
		}

		private void OnPatchRadialSoftnessSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.PatchRadialSoftness = (float)PatchRadialSoftnessSlider.Value;
		}

		private void OnSlatColour_ColorChanged(object sender, ColorChangedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.SlatColour = new System.Numerics.Vector3(SlatColour.Color.R / 255f, SlatColour.Color.G / 255f, SlatColour.Color.B / 255f);
		}

		private void OnSlatHeightSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.SlatHeight = (float)SlatHeightSlider.Value;
		}

		private void OnSlatSliceHeightSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.SlatSliceHeight = (float)SlatSliceHeightSlider.Value;
		}

		private void OnSlatOffsetSlider_ValueChanged(object sender, RoutedEventArgs e)
		{
			if (!init) return;
			EyeOverlays.SlatSliceOffset = (float)SlatOffsetSlider.Value;
		}

	}
}
