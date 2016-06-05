/*
 * AltInput: Alternate input plugin for Kerbal Space Program
 * Copyright © 2014 Pete Batard <pete@akeo.ie>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

// IMPORTANT: To be able to work with Unity, which is *BROKEN*,
// you must have a patched version of SharpDX.DirectInput such
// as the one provided with this project (in Libraries/)
// See: https://github.com/sharpdx/SharpDX/issues/406
using SharpDX.DirectInput;

namespace AltInput
{
    public enum MappingType
    {
        Range,
        Absolute,
        Delta,
    }

    public enum ControlType
    {
        Axis,
        OneShot,
        Continuous,
    }

    /// <summary>
    /// The range of a axis
    /// </summary>
    public struct AltRange
    {
        public int Minimum;
        public int Maximum;
        /// <summary>The range expressed as a floating point value to speed up computation</summary>
        public float FloatRange;
    }

    public struct AltMapping
    {
        /// <summary>Type of the mapping</summary>
        public MappingType Type;
        /// <summary>Name of the KSP game action this control should map to</summary>
        public String Action;
        /// <summary>Value the action should take, in case of an axis</summary>
        public float Value;
    }

    public struct AltControl
    {
        public ControlType Type;
        public Boolean Inverted;
        /// <summary>Dead zone, in the range 0.0 through 1.0, where 0.0 indicates that there
        /// is no dead zone, 0.5 indicates that the dead zone extends over 50 percent of the
        /// physical range of the axis and 1.0 indicates that the entire physical range of
        /// the axis is dead. For regular axes, the dead zone is applied to the center. For
        /// sliders, to the edges.</summary>
        public float DeadZone;
        /// <summary>Factor by which to multiply this input</summary>
        public float Factor;
    }

    /// <summary>
    /// An axis on the input device
    /// </summary>
    public struct AltAxis
    {
        /// <summary>Whether this axis is available on this controller</summary>
        public Boolean isAvailable;
        /// <summary>The range of this axis</summary>
        public AltRange Range;
        /// <summary>Last recorded value (to detect transitions)</summary>
        public float LastValue;
        /// <summary>The control determines how we should handle the axis</summary>
        public AltControl[] Control;
        // TODO: move Mapping inside the control struct?
        public AltMapping[] Mapping1;   // Regular mapping or Min mapping for thresholded values
        public AltMapping[] Mapping2;   // Max mapping for thresholded values
    }

    /// <summary>
    /// A Direct Input Device (typically a game controller)
    /// </summary>
    public class AltDirectInputDevice : AltDevice
    {
        /// <summary>Names for the axes. Using a double string array allows to map not so
        /// user-friendly DirectInput names to more user-friendly config counterparts.</summary>
        public readonly static String[,] AxisList = new String[,] {
            { "X", "AxisX" }, { "Y", "AxisY" }, { "Z", "AxisZ" },
            { "RotationX", "RotationX" }, { "RotationY", "RotationY" }, { "RotationZ", "RotationZ" },
            { "Sliders0", "Slider1" }, { "Sliders1", "Slider2" } };
        public DeviceClass Class;
        public Guid InstanceGuid;
        /// <summary>Default dead zone, in the range 0.0 through 1.0, where 0.0 indicates that
        /// there is no dead zone, 0.5 indicates that the dead zone extends over 50 percent of
        /// the physical range of the axis and 1.0 indicates that the entire physical range of
        /// the axis is dead. For regular axes, the dead zone is applied to the center. For
        /// sliders, to the edges.</summary>
        public float DeadZone;
        /// <summary>Default sensitivity of the device</summary>
        public float Factor = 1.0f;
        public AltAxis[] Axis;
        public Joystick Joystick;

        public AltDirectInputDevice(DirectInput directInput, DeviceClass deviceClass, Guid instanceGUID)
        {
            if (deviceClass != DeviceClass.GameControl)
                throw new ArgumentException("Class must be 'GameControl'");
            this.InstanceGuid = instanceGUID;
            this.Joystick = new Joystick(directInput, instanceGUID);
            this.enabledModes = new Boolean[GameState.NumModes];
            this.enabledModes[0] = true;
            this.Axis = new AltAxis[AltDirectInputDevice.AxisList.GetLength(0)];
            for (var i = 0; i < this.Axis.Length; i++)
            {
                this.Axis[i].Control = new AltControl[GameState.NumModes];
                this.Axis[i].Mapping1 = new AltMapping[GameState.NumModes];
                this.Axis[i].Mapping2 = new AltMapping[GameState.NumModes];
            }
        }

        public override void ProcessInput()
        {
            uint CurrentMode = (uint)GameState.CurrentMode;
            Joystick.Poll();
            var data = Joystick.GetBufferedData();
            foreach (var state in data)
            {
                // Only mapping axis
                if (state.Offset > JoystickOffset.Sliders1) continue;

                String OffsetName = Enum.GetName(typeof(JoystickOffset), state.Offset);
                // TODO(jtmcdole): Use a map to match values (my C# is very weak).
                for (var i = 0; i < AltDirectInputDevice.AxisList.GetLength(0); i++)
                {
                    if ((!Axis[i].isAvailable) || (String.IsNullOrEmpty(Axis[i].Mapping1[CurrentMode].Action)))
                        continue;
                    if (OffsetName == AltDirectInputDevice.AxisList[i,0])
                    {
                        float value = ((state.Value - Axis[i].Range.Minimum) /
                            (0.5f * Axis[i].Range.FloatRange)) - 1.0f;
                        if (Axis[i].Control[CurrentMode].Inverted)
                            value = -value;
                        // Because we computed some stuff, we need to apply the dead zone ourselves.
                        // Also a slider's dead zone applies to the edges rather than the center.
                        if (OffsetName.StartsWith("Slider"))
                        {
                            if (value < (-1.0f + Axis[i].Control[CurrentMode].DeadZone))
                                value = -1.0f;
                            if (value > (1.0f - Axis[i].Control[CurrentMode].DeadZone))
                                value = 1.0f;
                        }
                        else
                        {
                            if (Math.Abs(value) < Axis[i].Control[CurrentMode].DeadZone)
                                value = 0.0f;
                        }

                        switch (Axis[i].Control[CurrentMode].Type)
                        {
                            case ControlType.Axis:
                                GameState.UpdateAxis(Axis[i].Mapping1[CurrentMode], value, Axis[i].Control[CurrentMode].Factor);
                                break;
                            default:
                                print("AltInput: DirectInputDevice.ProcessInput() - unhandled control type");
                                break;
                        }
                        Axis[i].LastValue = value;
                    }
                }
            }
        }

        public override void OpenDevice()
        {
            // Device may have been already acquired - release it
            Joystick.Unacquire();
            ScreenMessages.PostScreenMessage("AltInput: Using Controller '" +
                Joystick.Information.InstanceName + "'", 10f, ScreenMessageStyle.UPPER_LEFT);
            // Set BufferSize in order to use buffered data.
            Joystick.Properties.BufferSize = 128;
            Joystick.Acquire();
        }

        public override void CloseDevice()
        {
            Joystick.Unacquire();
        }

        /// <summary>
        /// Resets all buttons and axes. This is used when switching game modes
        /// </summary>
        public override void ResetDevice()
        {
            uint m = (uint)GameState.CurrentMode;

            for (var i = 0; i < Axis.Length; i++)
            {
                // We don't touch the throttle
                if (Axis[i].isAvailable && (!Axis[i].Mapping1[m].Action.EndsWith("Throttle")))
                    GameState.UpdateAxis(Axis[i].Mapping1[m], 0.0f, Axis[i].Control[m].Factor);
            }
        }
    }
}
