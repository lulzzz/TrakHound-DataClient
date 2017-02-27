// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Data;
using TrakHound.Api.v2.Streams.Data;

namespace TrakHound.DataClient.DataGroups
{
    /// <summary>
    /// Defines what data to capture and how it is captured to be sent to a DataServer
    /// </summary>
    public class DataGroup
    {
        /// <summary>
        /// The Name of the DataGroup
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The CaptureMode of the DataGroup
        /// </summary>
        [XmlAttribute("captureMode")]
        public CaptureMode CaptureMode { get; set; }

        /// <summary>
        /// List of Allowed Types to capture
        /// </summary>
        [XmlArray("Allow")]
        [XmlArrayItem("Filter")]
        public List<string> Allowed { get; set; }

        /// <summary>
        /// List of Denied Types to not capture
        /// </summary>
        [XmlArray("Deny")]
        [XmlArrayItem("Filter")]
        public List<string> Denied { get; set; }

        /// <summary>
        /// List of other DataGroups to include when capturing for this group
        /// </summary>
        [XmlArray("Include")]
        [XmlArrayItem("DataGroup")]
        public List<string> IncludedDataGroups { get; set; }

        /// <summary>
        /// Check a Sample based on the DataGroup's filters
        /// </summary>
        /// <param name="sample">The Sample to check</param>
        /// <returns>A boolean indicating whether or not the Sample passes the filters</returns>
        public bool CheckFilters(SampleData sample)
        {
            List<DataItemDefinitionData> dataItemDefinitions = null;
            List<ComponentDefinitionData> componentDefinitions = null;

            lock(DataClient._lock)
            {
                dataItemDefinitions = DataClient.DataItemDefinitions.ToList();
                componentDefinitions = DataClient.ComponentDefinitions.ToList();
            }

            if (sample != null && dataItemDefinitions != null && componentDefinitions != null)
            {
                string deviceId = sample.DeviceId;

                var dataDefinition = dataItemDefinitions.Find(o => o.DeviceId == deviceId && o.Id == sample.Id);
                if (dataDefinition != null)
                {
                    bool match = Allowed == null || Allowed.Count == 0;

                    // Search Allowed Filters
                    foreach (var filter in Allowed)
                    {
                        var dataFilter = new DataFilter(filter, dataDefinition, componentDefinitions.ToList<ComponentDefinition>());
                        match = dataFilter.IsMatch();
                        if (match) break;
                    }

                    if (match)
                    {
                        // Search Denied Filters
                        foreach (var filter in Denied)
                        {
                            var dataFilter = new DataFilter(filter, dataDefinition, componentDefinitions.ToList<ComponentDefinition>());
                            bool denied = dataFilter.IsMatch();
                            if (denied)
                            {
                                match = false;
                                break;
                            }
                        }
                    }

                    return match;
                }
            }

            return false;
        }
        
    }
}
