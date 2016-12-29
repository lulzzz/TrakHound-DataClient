// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect.Headers;
using System;

namespace TrakHound.DataClient.Data
{
    /// <summary>
    /// Defines an MTConnect Device
    /// </summary>
    public class AgentDefinition : Api.v2.Data.AgentDefinition
    {
        public AgentDefinition()
        {
            EntryId = Guid.NewGuid().ToString();
        }

        public AgentDefinition(string deviceId, MTConnectDevicesHeader header)
        {
            // TrakHound Properties
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;

            // MTConnect Properties
            InstanceId = header.InstanceId;
            Sender = header.Sender;
            Version = header.Version;
            CreationTime = header.CreationTime;
            BufferSize = header.BufferSize;
            TestIndicator = header.TestIndicator;
        }
    }
}
