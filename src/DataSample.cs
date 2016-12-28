// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect;
using MTConnect.MTConnectStreams;
using Newtonsoft.Json;
using System;
using TrakHound.Api.v2;

namespace TrakHound.DataClient
{
    public class DataSample : Samples.Sample
    {
        [JsonIgnore]
        public string Uuid { get; set; }

        [JsonIgnore]
        public string Type { get; set; }

        public DataSample() { }

        public DataSample(string deviceId, DataItem dataItem)
        {
            Uuid = Guid.NewGuid().ToString();
            DeviceId = deviceId;

            Id = dataItem.DataItemId;
            Type = dataItem.Type;
            Timestamp = dataItem.Timestamp;

            if (dataItem.Category == DataItemCategory.CONDITION) Value2 = ((Condition)dataItem).ConditionValue.ToString();

            Value1 = dataItem.CDATA;
        }

        public string ToCsv()
        {
            string f = "{0},{1},{2},{3},{4},{5}";
            return string.Format(f, Uuid, DeviceId, Id, Value1, Value2, Timestamp.ToString("o"));
        }

        public static DataSample FromCsv(string line)
        {
            var fields = line.Split(',');
            if (fields != null && fields.Length == 6)
            {
                var sample = new DataSample();
                sample.Uuid = fields[0];
                sample.DeviceId = fields[1];
                sample.Id = fields[2];
                sample.Value1 = fields[3];
                sample.Value2 = fields[4];

                string t = fields[5];
                DateTime timestamp;
                if (DateTime.TryParse(t, out timestamp))
                {
                    sample.Timestamp = timestamp;
                    return sample;
                }
            }

            return null;
        }
    }
}
