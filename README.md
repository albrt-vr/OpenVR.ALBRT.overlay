# OpenVR.ALBRT.overlay
An overlay application for OpenVR (SteamVR)
----

This organisation's repositories, and this repository, contains technical demonstrations of software based visual deltas within game engines utilising stereo rendering APIs and stand alone stereo rendering APIs. All content is for educational purposes only. Nothing present is intended to be used as a product or for any purpose beyond technical reference. Everything is provided by its respective contributor as-is with no warranty, guarantee, or liability; for misuse, damage, or any detriment to any computer system, person, or thing.

The software examples and demos involve the use of stereo rendering techniques present in APIs, libraries, and product SDKs. All software examples and demos are extensions or uses of stereo rendering available in these APIs and do not constitute a product or device in their own right. No exclusive access, exclusive rights, or API modifications are used in any way within any of the software examples or demos. Any disputes or concerns regarding these APIs should be forwarded to the respective owner of the API in question. e.g. Unity, Valve, Meta, Khronos Group (this is a non exhaustive list).

Please read all licensing documents and files, including those in sub directories, before downloading any part of any repository from this organisation.

**AGREEMENT:** Any usage or execution of any computer code, apps, programs, files, demos, games, executables, or any part of the computer code, in whole or in part, within these repositories incurs no warranty, guarantee, or liability; for misuse, damage, or any detriment to any computer system, person, or thing. If you download any of these files, in whole or in part, you are agreeing to every statement and declaration made on this page.

**ADVISORY:** If you do not understand what these tools and demos do in detail, do not use any of them in any way for any purpose.

----

**IMPORTANT:** The cross platform builds have been removed due to the effort of testing, and other reasons relating to window rendering which broke the SteamVR window manager. Scroll to the bottom to read a summary.

(Note this README is synced with the staging repo so you may not yet see the content referenced within)

# Notes About This App
This overlay runs as a desktop window, it does not run as a dashboard overlay, as the options for desktop window overlays currently exceed those for dashboard overlays within SteamVR.

This app runs on Windows 10+ 64 bit. (This may change; depending on testing)

This is not a signed app and it has no installer. (This may change; depending on framework used)

This app will never be distributed BY us on any other platform or installer. If in doubt about the source of your version, you should re-download from this repository, or a trustworthy fork.

This app is licensed under the MIT licence, and uses OpenVR which is under the BSD-3-Clause licence. If you make substantial improvements we ask kindly that you fork from this repository and contribute your changes via a pull request (You are under no obligation to do this under the aforementioned licences, it is a request only).

# How To Use This App
Download the newest release from the releases page, and run the ALBRT.overlay.exe; it is advisable to have a shortcut somewhere easily accessible from within the VR desktop view, as this app must be started from outside of Steam.

YOU SHOULD NOT ADD THIS AS A GAME IN YOUR STEAM LIBRARY. It is not a stand alone game, and running it as a game will prevent it from working as intended. It must be run from your system so it can initialise as an OpenVR overlay app.

You should not run multiple instances of this app as it has hard coded overlay keys. Close all duplicate instances. (If there is a use case for this please submit and issue and explain)

There is a directory within the app’s root directory called ‘Image Masks’ which can be modified or added to. Please see the section ‘Modify the Masks’.

To open the settings window within VR; open the SteamVR dashboard, press [window icon] 'Desktops' then in the bottom right [(+) circled plus icon] 'Add View'. You may then pin the UI to your controller by pressing the controller icons, or place the window within the VR world by holding on the [+ cross arrows icon] and dragging it around. I suggest placing the UI behind you within the scene.

# Modify the Masks
You must use a .png (32 bit, ie with transparency) and I advise a resolution of 2048 or 4096 squared. Non square images will result in undefined and broken behaviour within the render. For clarity, a square image means one with an equal width and height, ie 4096x4096px.

You must name mask pairs with the same base filename, and add a suffix of .a and .b to each filename. For example cat.a.png & cat.b.png. These will then be shown as a mask pair within the settings window. Mask pairs will start rendering mask ‘a’ on the left, and mask ‘b’ on the right. Press the ‘switch eyes’ option in the settings window to change this. If you wish to permanently switch this, simply rename the suffixes on the mask pair, ie a->b and b->a.

For single masks you may name the files in any way you wish. For example a_random_name_here.png. This will be shown as a single mask and will only apply to a single lens. Single masks start rendering on the left hand side. Press the ‘switch eyes’ option in the settings window to change this. If you wish to permanently set one side, you may add the suffix ‘r’ or ‘l’ to the file name, ie mask.r.png. This will automatically render the single mask on the chosen side.

All colours and transparency within the .png will be rendered, so ensure your source images have all of the desired values. The overall transparency of the masks can be changed within the settings window.

# Errors
### [E:0] No HMD / SteamVR not running
You must ensure SteamVR is running before opening the app. You must ensure an HMD is connected before running the app.

Solution: 

1. Ensure SteamVR is working with an HMD connected. 
2. Re-run the app.

### [E:1] OpenVR overlay app failed
The OpenVR overlay app initialisation has failed.

Solution: 

1. Restart SteamVR.
2. Re-run the app.

### [E:2] Overlays error / Running twice
(Generic) Something went wrong during overlay rendering. Or two instances of this app are trying to manage the overlays.

Solution: 
1. Ensure the app has not been run multiple times.
2. Close any instances of the app reporting the error and use the one that works.

If not running twice:

1. Restart SteamVR.
2. Re-run the app.

### [E:3] Dashboard overlay error / Running twice
NOTE: There is no dashboard rendering in the current app - use desktop window pinning instead.

(Generic) Something went wrong during dashboard overlay rendering. Or two instances of this app are trying to manage the overlays.

Solution: 
1. Ensure the app has not been run multiple times.
2. Close any instances of the app reporting the error and use the one that works.

If not running twice:

1. Restart SteamVR.
2. Re-run the app.

# Notes on Cross Platform Versions
I wrote this app in cpp, and gave up due to the UI requirements. I then moved to QT (major issues), and then to Avalonia (window rendering disallows SteamVR window mgr to see them). I considered MAUI but it lacks Linux build support and requires packaged releases. If you wish to help port this to Linux please feel free to fork and improve. I will probably work on the cpp version as a minimalist app with config files instead of a UI; but for now it is removed.

API SOURCES: The cpp OpenVR API is available at the OpenVR repo, and the C# api is available within this repo or as part of the Unity SteamVR plugin. I encourage you to play with them yourself as overlays are incredibly simple.
