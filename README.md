AltInput: Alternate Input plugin for Kerbal Space Program
==========================================================

Description
--------------------------------------

This is a very stripped down fork of AltInput in order to just work with the
Microsoft SideWinder Force Feedback Pro 2 (axial correction). Unity only reads
the raw input for a controller and the center position on the FF2 is accurate
enought to cause Unity to report hard-left roll and yaw, and hard-down pitch.

Why did I get rid of everything else? Beacuse you should be using the
Advanced Fly By Wire mod! You can edit the button bindings live, rotate between
presets, etc.

[AFBW](http://forum.kerbalspaceprogram.com/index.php?/topic/85711-11-afbw-v17-beta-joystick-controller-mod/http://forum.kerbalspaceprogram.com/index.php?/topic/85711-11-afbw-v17-beta-joystick-controller-mod/)

Currently it only does so for [DirectInput](http://msdn.microsoft.com/en-us/library/windows/desktop/ee416842.aspx)
Joysticks, and as such will only work on Windows. 
But it could be extended to support any other kind of input.

__IMPORTANT:__ This module provides its OWN version of the [SharpDX](http://sharpdx.org/)
DLLs, due to a [BUG](https://github.com/sharpdx/SharpDX/issues/406) in the Unity engine.
If you try to deploy with the standard version of SharpDX, it __WILL NOT WORK!__


Compilation
--------------------------------------

* Open `AltInput.csproj` with a text editor and update the following section
according to your path: 
```
  <PropertyGroup>
    <KSPPath>C:\Program Files\Steam\SteamApps\common\Kerbal Space Program</KSPPath>
  </PropertyGroup>
```
* Open the project and compile. If you edited the `KSPPath` variable above, all
  the required module files will be copied automatically.


Usage
--------------------------------------

* If needed, copy `AltInput.dll`, `SharpDX.dll` and `SharpDX.DirectInput.dll` into the KSP 
 `Plugins` directory (typically `C:\Program Files (x86)\Steam\SteamApps\common\Kerbal Space Program\Plugins`)
* Copy the whole `PluginData` directory to the same directory where you copied 
  the DLL files and if needed, edit `PluginData\AltInput\config.ini` according 
  to your settings.
* Play. ;) If your joystick is properly configured, you will get a notification 
  (in the upper left corner) and you should be able to use it to control the 
  ship. Or, if you need to create a new entry, you will also be able to find the 
  name you should use to create a new controller section.


Configuration
--------------------------------------

All configuration is done in `PluginData\AltInput\config.ini`

The following DirectInput controls, if present, can be assigned to Vessel Control.
These are the names you should use for `Control` when adding a 
`Control = Mapping` line in your config file:
* `AxisX`
* `AxisY`
* `AxisZ`
* `RotationX`
* `RotationY`
* `RotationZ`
* `Slider[1-2]`

Anything that isn't in the list above is not handled.

The following KSP actions can be mapped to an input. These are the names you should
use for `Mapping` when adding a `Control = Mapping` line in your config file:
* `yaw`
* `pitch`
* `roll`
* `mainThrottle`
* `X` - Translation X
* `Y` - Translation Y
* `Z` - Translation Z

How do I find the names I should use for my controls?
--------------------------------------

The control names should follow what Windows report when checking your game
controller properties. For instance, using Windows 8, if you go to _Device and
Printers_ you should be able to access the _Game controller settings_ menu by
right clicking on your device. Then, if you select _Properties_, you will be
presented with a dialog where all the buttons and axes your controller provides
are labelled. Matching these names with the ones from the first list above is
then a trivial matter.

__Hint:__ You can also easily identify which button is which in this dialog by
pressing them and writing down the number that gets highlighted then.

FAQ
--------------------------------------

### Why don't you use use Unity's _Input_, instead of reinventing the wheel?

Unity's _Input_ doesn't support POV (Point of View) hats. At all. Also it uses
`DirectRaw` to access controllers, which is super sucky. And you can also forget
about using a slider to control a throttle with Unity's _Input_.

The Unity developers have repeatedly indicated that they have NO INTENTION of
addressing these serious shortcomings, so this leaves very little choice...
