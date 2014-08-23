﻿/*
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
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using UnityEngine;
using KSP.IO;

namespace AltInput
{
    /// <summary>
    /// Handles the input device in-flight actions
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ProcessInput : MonoBehaviour
    {
        private GameState gs = null;

        /// <summary>
        /// Update the flight control state according to our inputs
        /// </summary>
        /// <param name="CurrentState">The current control state for the active vessel</param>
        private void ControllerInput(FlightCtrlState CurrentState)
        {
            // Does this ever occur?
            if (FlightGlobals.ActiveVessel == null)
            {
                gs = null;
                // No need to do anything
                return;
            }
            if (Config.DeviceList.Count != 0)
            {
                if (gs == null)
                    gs = new GameState(CurrentState);
                foreach (var Device in Config.DeviceList)
                {
                    Device.ProcessInput(gs);
                    gs.UpdateState(CurrentState);
                }
            }
        }

        public void Start()
        {
#if (DEBUG)
            print("AltInput: ProcessInput.Start()");
#endif
            // TODO: only list/acquire controller if we have some mapping assigned
            foreach (var Device in Config.DeviceList)
                Device.OpenDevice();
            if (Config.DeviceList.Count == 0)
                ScreenMessages.PostScreenMessage("AltInput: No controller detected", 10f,
                    ScreenMessageStyle.UPPER_LEFT);
            // Add our handler
            Vessel ActiveVessel = FlightGlobals.ActiveVessel;
            if (ActiveVessel != null)
                ActiveVessel.OnFlyByWire += new FlightInputCallback(ControllerInput);
        }

        void OnDestroy()
        {
#if (DEBUG)
            print("AltInput: ProcessInput.OnDestroy()");
#endif
            Vessel ActiveVessel = FlightGlobals.ActiveVessel;
            if (ActiveVessel != null)
                ActiveVessel.OnFlyByWire -= new FlightInputCallback(ControllerInput);
            foreach (var Device in Config.DeviceList)
                Device.CloseDevice();
        }
    }
}