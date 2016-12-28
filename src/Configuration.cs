// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TrakHound.DataClient
{
    [XmlRoot("DataClient")]
    public class Configuration
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        [XmlIgnore]
        public const string FILENAME = "client.config";

        [XmlIgnore]
        public string Path { get; set; }

        [XmlElement("DeviceFinder")]
        public DeviceFinder.DeviceFinder DeviceFinder { get; set; }

        [XmlArray("Devices")]
        [XmlArrayItem("Device", typeof(Device))]
        public List<Device> Devices { get; set; }

        [XmlArray("DataServers")]
        [XmlArrayItem("DataServer")]
        public List<DataServer> DataServers { get; set; }

        public Configuration()
        {
            Devices = new List<Device>();
            DataServers = new List<DataServer>();
        }

        public static Configuration Get(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(Configuration));
                    using (var fileReader = new FileStream(path, FileMode.Open))
                    using (var xmlReader = XmlReader.Create(fileReader))
                    {
                        var config = (Configuration)serializer.Deserialize(xmlReader);
                        config.Path = path;
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }           

            return null;
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty(Path))
            {
                try
                {
                    var settings = new XmlWriterSettings();
                    settings.Indent = true;
                    var serializer = new XmlSerializer(typeof(Configuration));
                    using (var fileWriter = new FileStream(Path, FileMode.Create))
                    using (var xmlWriter = XmlWriter.Create(fileWriter, settings))
                    {
                        serializer.Serialize(xmlWriter, this);
                    }

                    log.Info("Configuration Saved : " + Path);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            else log.Warn("Configuration could not be saved. No Path is set.");
        }
    }
}
