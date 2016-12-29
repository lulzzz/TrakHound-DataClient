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

        public DataItemDefinition(string deviceId, DataItem dataItem)
        {
            Init(deviceId, dataItem, null);
        }

        public DataItemDefinition(string deviceId, DataItem dataItem, string parentId)
        {
            Init(deviceId, dataItem, parentId);
        }

        private void Init(string deviceId, DataItem dataItem, string parentId)
        {
            // TrakHound Properties
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;
            ParentId = parentId;

            // MTConnect Properties
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
