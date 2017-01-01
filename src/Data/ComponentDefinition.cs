// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using MTConnectDevices = MTConnect.MTConnectDevices;

namespace TrakHound.DataClient.Data
{
    public class ComponentDefinition : Api.v2.Data.ComponentDefinition
    {
        public ComponentDefinition()
        {
            EntryId = Guid.NewGuid().ToString();
        }

        public ComponentDefinition(string deviceId, MTConnectDevices.IComponent component, string parentId, string agentInstanceId, DateTime timestamp)
        {
            // TrakHound Properties
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;
            ParentId = parentId;
            Timestamp = timestamp;

            // MTConnect Properties
            AgentInstanceId = agentInstanceId;
            Component = component.GetType().Name;
            Id = component.Id;
            Uuid = component.Uuid;
            Name = component.Name;
            NativeName = component.NativeName;
            SampleInterval = component.SampleInterval;
            SampleRate = component.SampleRate;
        }
    }
}
