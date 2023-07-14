# OpenVR.ALBRT.overlay
An overlay application for OpenVR (SteamVR)
----

This organisation's repositories, and this repository, contains technical demonstrations of software based visual deltas within game engines utilising stereo rendering APIs and stand alone stereo rendering APIs. All content is for educational purposes only. Nothing present is intended to be used as a product or for any purpose beyond technical reference. Everything is provided by its respective contributor as-is with no warranty, guarantee, or liability; for misuse, damage, or any detriment to any computer system, person, or thing.

The software examples and demos involve the use of stereo rendering techniques present in APIs, libraries, and product SDKs. All software examples and demos are extensions or uses of stereo rendering available in these APIs and do not constitute a product or device in their own right. No exclusive access, exclusive rights, or API modifications are used in any way within any of the software examples or demos. Any disputes or concerns regarding these APIs should be forwarded to the respective owner of the API in question. e.g. Unity, Valve, Meta, Khronos Group (this is a non exhaustive list).

Please read all licensing documents and files, including those in sub directories, before downloading any part of any repository from this organisation.

**AGREEMENT:** Any usage or execution of any computer code, apps, programs, files, demos, games, executables, or any part of the computer code, in whole or in part, within these repositories incurs no warranty, guarantee, or liability; for misuse, damage, or any detriment to any computer system, person, or thing. If you download any of these files, in whole or in part, you are agreeing to every statement and declaration made on this page.

**ADVISORY:** If you do not understand what these tools and demos do in detail, do not use any of them in any way for any purpose.

----

# Notes About This App
Windows 10+ 64 bit.

This is not a signed app and it has no installer.

This app will never be distributed by us on any other platform or installer. If in doubt about the source of your version, you should re-download from this repository, or a trustworthy fork.

This app is licensed under the MIT licence, and uses OpenVR which is under the BSD-3-Clause licence. If you make substantial improvements we ask kindly that you fork from this repository and contribute your changes via a pull request (You are under no obligation to do this under the aforementioned licences, it is a request only).

# How To Use This App
Download the newest release zip from the [Releases page](https://github.com/albrt-vr/OpenVR.ALBRT.overlay/releases), or the Builds directory, extract it somewhere on disk that you intend to keep this app, make sure SteamVR is running, then run ALBRT.overlay.win64.exe from the ALBRT.overlay directory. Please DO NOT change the build directory structure. It should look like: ALBRT.overlay_vn-n-n/ALBRT.overlay/[content including the .exe]. It is important that this structure is kept so SteamVR can load the app.

If you move the application directory after launching it for the first time SteamVR will lose the location of the VR manifest, and you will have to run and close SteamVR at least once for it to realise and 'forget' the old location. Once you have done this you should be able to launch ALBRT.overlay again and SteamVR will re-register its new location correctly.

If you are running the app for the first time you will be asked to download the new windows app runtime. If you have issues with windows not downloading it when you accept, it can be found here: 
https://aka.ms/windowsappsdk/1.3/1.3.230602002/windowsappruntimeinstall-x64.exe

YOU SHOULD NOT ADD THIS AS A GAME IN YOUR STEAM LIBRARY. It is not a game, and running it as a game will prevent it from working as intended. It must be run from your system so it can initialise as an OpenVR overlay app.

If you wish for this app to load when SteamVR loads, run it and look in the SteamVR dashboard Settings>Startup/Shutdown menu for this overlay app. If you move the app or upgrade it, you must follow the steps laid out in the section "Checking the Version and Upgrading" to re-link to SteamVR.

You must run this app after SteamVR is running or it will not be able to connect.

You should not run multiple instances of this app as it has hard coded overlay keys. Close all duplicate instances. (If there is a use case for this please submit an issue and explain)

To open the app window within VR; open the SteamVR dashboard, press [window icon] 'Desktops' then in the bottom right [(+) circled plus icon] 'Add View'. You may then pin the UI to your controller by pressing the controller icons, or place the window within the VR world by holding on the [+ cross arrows icon] and dragging it around.

# Checking the Version and Upgrading
To check the version open the task manager and right click the application process and click "properties". Then go to the "Details" tab and look for "Product Version".

When you upgrade by downloading a new version, if the directory structure changes (it will as our master dir has the version string in as this is a floating app) you will lose the link to the VR manifest. You will have to run and close SteamVR at least once for it to realise and 'forget' the old location. Once you have done this you should be able to launch ALBRT.overlay again and SteamVR will re-register its new location correctly.

I apologise for this annoyance, but SteamVR expects apps to be installed into steam, and not into floating directories, so has no tools to aid in re-linking nicely.

# Settings
If you wish to reset everything, delete the settings.json file from the app's root directory. You may edit the settings file but be careful not to make errors or you will lose the file as it will be removed when the app is used.

The settings file includes a hidden image mode ("ImageModeActive": false,) that you can set to true if you wish to use images instead of the procedural overlays. Image ImageMasks/a.png is left, and ImageMasks/b.png is right. Only the alpha settings will apply when in this mode. Also, please note the image format and resolution, and the fact there is a central transparent 'gutter' to avoid UV leaking.

# Errors
NOTE there are OpenVR error names provided alongside most errors. Make sure you note them as well as these error codes.

### [E:0] SteamVR not running / No HMD
You must ensure SteamVR is running before opening the app. You must ensure an HMD is connected before running the app.

Solution: 

1. Ensure SteamVR is working with an HMD connected. 
2. Re-run the app.

### [E:1] OpenVR overlay app failed / No HMD
The OpenVR overlay app initialisation has failed. You must ensure an HMD is connected before running the app.

Solution: 

1. Ensure SteamVR is working with an HMD connected. 
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

# Known Issues
If you launch the app multiple times before pinning the desktop window within the SteamVR dashboard, adding the app window will cause undefined behaviour within the SteamVR dashboard.

# Notes on Cross Platform Versions
I wrote this app in cpp, and gave up due to the UI requirements. I then moved to QT (major issues), and then to Avalonia (window rendering disallows SteamVR window mgr to see them). I considered MAUI but it lacks Linux build support and requires packaged releases. If you wish to help port this to Linux please feel free to fork and improve.

API SOURCES: The cpp OpenVR API is available at the OpenVR repo, and the C# api is available within this repo or as part of the Unity SteamVR plugin. I encourage you to play with them yourself as overlays are incredibly simple.
