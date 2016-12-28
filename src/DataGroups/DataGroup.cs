// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

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


        public bool CheckFilters(DataSample sample)
        {
            string deviceId = sample.DeviceId;

            var dataDefinition = DataClient.DataDefinitions.Find(o => o.DeviceId == deviceId && o.Id == sample.Id);
            if (dataDefinition != null)
            {
                bool match = false;

                // Search Allowed Filters
                foreach (var filter in Allowed)
                {
                    match = CheckFilter(dataDefinition, filter);
                    if (match) break;
                }

                if (match)
                {
                    // Search Denied Filters
                    foreach (var filter in Denied)
                    {
                        bool denied = CheckFilter(dataDefinition, filter);
                        if (denied)
                        {
                            match = false;
                            break;
                        }
                    }
                }

                return match;
            }

            return false;
        }

        private bool CheckFilter(DataDefinition dataDefinition, string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                string deviceId = dataDefinition.DeviceId;

                var paths = filter.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (paths != null)
                {
                    string id = dataDefinition.ParentId;
                    if (!string.IsNullOrEmpty(id))
                    {
                        bool match = false;

                        for (var i = paths.Length - 1; i >= 0; i--)
                        {
                            var path = paths[i];

                            // If Last Node in Path
                            if (i == paths.Length - 1)
                            {
                                if (HasWildcard(filter)) match = true;
                                else
                                {
                                    match = NormalizeType(dataDefinition.Type) == NormalizeType(path);
                                    if (!match) match = dataDefinition.Id == path;
                                }
                            }
                            else
                            {
                                var containerDefinition = DataClient.ContainerDefinitions.Find(o => o.DeviceId == deviceId && o.Id == id);
                                if (containerDefinition != null)
                                {
                                    id = containerDefinition.ParentId;
                                    match = NormalizeType(containerDefinition.Type) == NormalizeType(path);
                                    if (!match) match = containerDefinition.Id == path;
                                }
                            }

                            if (!match) break;
                        }

                        return match;
                    }
                }
            }
            
            return false;
        }
        
        private static string NormalizeType(string s)
        {
            string debug = s;

            if (!string.IsNullOrEmpty(s))
            {
                if (s.ToUpper() != s)
                {
                    // Split string by Uppercase characters
                    var parts = Regex.Split(s, @"(?<!^)(?=[A-Z])");
                    s = string.Join("_", parts);
                    s = s.ToUpper();
                }

                // Return to Pascal Case
                s = s.Replace("_", " ");
                s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());

                s = s.Replace(" ", "");

                return s;
            }

            return s;
        }

        private static bool HasWildcard(string filter)
        {
            return filter.Length > 0 && filter[filter.Length - 1] == '*';
        }
    }
}
