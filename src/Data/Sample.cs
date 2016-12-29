// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect;
using MTConnect.MTConnectStreams;
using System;

namespace TrakHound.DataClient.Data
{
    /// <summary>
    /// A sample of data either from an MTConnect Sample or Current stream document
    /// </summary>
    public class Sample : Api.v2.Data.Sample
    {
        public Sample()
        {
            EntryId = Guid.NewGuid().ToString();
        }

        public Sample(string deviceId, DataItem dataItem)
        {
            EntryId = Guid.NewGuid().ToString();
            DeviceId = deviceId;

            Id = dataItem.DataItemId;
            Sequence = dataItem.Sequence;
            Timestamp = dataItem.Timestamp;
            CDATA = dataItem.CDATA;
            if (dataItem.Category == DataItemCategory.CONDITION) Condition = ((Condition)dataItem).ConditionValue.ToString();
        }

    }
}
