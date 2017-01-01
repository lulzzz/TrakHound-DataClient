// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using MTConnectDevices = MTConnect.MTConnectDevices;

namespace TrakHound.DataClient.Data
{
    /// <summary>
    /// Defines an MTConnect Device
    /// </summary>
    public class DeviceDefinition : Api.v2.Data.DeviceDefinition
    {
        public DeviceDefinition()
        {
            EntryId = Guid.NewGuid().ToString();
        }

        public DeviceDefinition(string deviceId, MTConnectDevices.Device device, string agentInstanceId, DateTime timestamp)
        {
            // TrakHound Properties
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;
            Timestamp = timestamp;

            // MTConnect Properties
            AgentInstanceId = agentInstanceId;
            Id = device.Id;
            Uuid = device.Uuid;
            Name = device.Name;
            NativeName = device.NativeName;
            SampleInterval = device.SampleInterval;
            SampleRate = device.SampleRate;
            Iso841Class = device.Iso841Class;
        }
    }
}
