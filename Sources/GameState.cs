/*
 * AltInput: Alternate input plugin for Kerbal Space Program
 * Copyright © 2014 Pete Batard <pete@akeo.ie>
 * Thanks go to zitronen for KSPSerialIO, which helped figure out
 * spacecraft controls in KSP: https://github.com/zitron-git/KSPSerialIO
 * TimeWarp handling from MechJeb2 by BloodyRain2k:
 * https://github.com/MuMech/MechJeb2/blob/master/MechJeb2/MechJebModuleWarpController.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using KSP.IO;

namespace AltInput
{
    public class GameState : MonoBehaviour
    {
        /// <summary>The game modes we support</summary>
        public enum Mode
        {
            Flight = 0,
            AltFlight,
            Ground,
        };
        public readonly static String[] ModeName = Enum.GetNames(typeof(Mode));
        public readonly static int NumModes = ModeName.Length;
        public static AltDevice CurrentDevice;

        public static FlightCtrlState UpdatedState = null;
        public static Mode CurrentMode = Mode.Flight;
        // Build a static list of all FlightCtrlState attributes that are of type float
        // and don't contain "Trim" in their name. These are the axes we will handle.
        public readonly static FieldInfo[] AxisFields =
            typeof(FlightCtrlState).GetFields().Where(item =>
                (item.FieldType == typeof(float)) && (!item.Name.Contains("Trim"))).ToArray();

        /// <summary>
        /// Update a Flight Vessel axis control (including throttle) by name
        /// </summary>
        /// <param name="Mapping">The mapping for the axis to update</param>
        /// <param name="value">The value to set, if override</param>
        /// <param name="factor">The factor for the computed value</param>
        public static void UpdateAxis(AltMapping Mapping, float value, float factor)
        {
            FieldInfo field = typeof(FlightCtrlState).GetField(Mapping.Action);
            if (field == null)
            {
                print("AltInput: '" + Mapping.Action + "' is not a valid Axis name");
                return;
            }
            Boolean isThrottle = Mapping.Action.EndsWith("Throttle");
            if (Mapping.Type == MappingType.Delta)
                value += (float)field.GetValue(UpdatedState);
            else if (isThrottle)
                value = (value + 1.0f) / 2.0f;

            value *= factor;
            value = Mathf.Clamp(value, isThrottle?0.0f:-1.0f, +1.0f);
            field.SetValue(UpdatedState, value);
        }

        /// <summary>
        /// Update the current state of the spacecraft according to all inputs
        /// </summary>
        /// <param name="CurrentState">The current flight control state</param>
        public static void UpdateState(FlightCtrlState CurrentState)
        {
            // Go through all our axes to find the ones we need to update
            foreach (FieldInfo field in AxisFields)
            {
                if (Math.Abs((float)field.GetValue(CurrentState)) < Math.Abs((float)field.GetValue(UpdatedState)))
                {
                    field.SetValue(CurrentState, (float)field.GetValue(UpdatedState));
                    // The throttles are a real PITA to override
                    if (field.Name == "mainThrottle")
                        FlightInputHandler.state.mainThrottle = 0.0f;
                    else if (field.Name == "wheelThrottle")
                        FlightInputHandler.state.wheelThrottle = 0.0f;
                }
            }
            // If SAS is on, we need to override it or else our changes are ignored
            VesselAutopilot.VesselSAS vesselSAS = FlightGlobals.ActiveVessel.Autopilot.SAS;
            Boolean overrideSAS = (Math.Abs(CurrentState.pitch) > vesselSAS.controlDetectionThreshold) ||
                                    (Math.Abs(CurrentState.yaw) > vesselSAS.controlDetectionThreshold) ||
                                    (Math.Abs(CurrentState.roll) > vesselSAS.controlDetectionThreshold);
            vesselSAS.ManualOverride(overrideSAS);
        }
    }
}
