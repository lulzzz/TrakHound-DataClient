// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Newtonsoft.Json;
using TrakHound.DataClient.Buffers;

namespace TrakHound.DataClient.Data
{
    /// <summary>
    /// Defines a type of data to be sent to a DataServer
    /// </summary>
    public abstract class Definition : IBufferData
    {
        /// <summary>
        /// A unique identifier to this particular Definition that was received
        /// </summary>
        [JsonIgnore]
        public string EntryId { get; set; }

        /// <summary>
        /// The identifier of the TrakHound Device that this Definition is related to
        /// </summary>
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The parent Agent, Device, or Component of this Definition
        /// </summary>
        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
    }
}
