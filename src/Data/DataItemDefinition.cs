// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect.MTConnectDevices;
using System;

namespace TrakHound.DataClient.Data
{
    public class DataItemDefinition : Api.v2.Data.DataItemDefinition
    {
        public DataItemDefinition()
        {
            EntryId = Guid.NewGuid().ToString();
        }

        public DataItemDefinition(string deviceId, DataItem dataItem, string agentInstanceId, DateTime timestamp)
        {
            Init(deviceId, dataItem, null, agentInstanceId, timestamp);
        }

        public DataItemDefinition(string deviceId, DataItem dataItem, string parentId, string agentInstanceId, DateTime timestamp)
        {
            Init(deviceId, dataItem, parentId, agentInstanceId, timestamp);
        }

        private void Init(string deviceId, DataItem dataItem, string parentId, string agentInstanceId, DateTime timestamp)
        {
            // TrakHound Properties
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;
            ParentId = parentId;
            Timestamp = timestamp;

            // MTConnect Properties
            AgentInstanceId = agentInstanceId;
            Id = dataItem.Id;
            Name = dataItem.Name;
            Catergory = dataItem.Category.ToString();
            Type = dataItem.Type;
            SubType = dataItem.SubType;
            Statistic = dataItem.Statistic;
            Units = dataItem.Units;
            NativeUnits = dataItem.NativeUnits;
            NativeScale = dataItem.NativeScale;
            CoordinateSystem = dataItem.CoordinateSystem;
            SampleRate = dataItem.SampleRate;
            Representation = dataItem.Representation;
            SignificantDigits = dataItem.SignificantDigits;
        }
        
    }
}
