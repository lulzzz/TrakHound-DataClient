// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TrakHound.DataClient
{
    public class DataClient
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();


        public static List<DataDefinition> DataDefinitions = new List<DataDefinition>();

        public static List<ContainerDefinition> ContainerDefinitions = new List<ContainerDefinition>();

        public static List<DataSample> CurrentSamples = new List<DataSample>();


        public Configuration Configuration { get; set; }


        public DataClient(string configPath)
        {
            PrintHeader();

            var config = Configuration.Get(configPath);
            if (config != null)
            {
                log.Info("Configuration file read from '" + configPath + "'");
                log.Info("---------------------------");

                LoadConfiguration(config);
            }
            else
            {
                // Throw exception that no configuration file was found
                var ex = new Exception("No Configuration File Found. Exiting TrakHound-DataClient!");
                log.Error(ex);
                throw ex;
            }
        }

        private void LoadConfiguration(Configuration config)
        {
            Configuration = config;

            // Start Devices
            foreach (var device in config.Devices)
            {
                device.ContainerDefinitionsReceived += ContainerDefinitionsReceived;
                device.DataDefinitionsReceived += DataDefinitionsReceived;
                device.DataSamplesReceived += DataSamplesReceived;
                device.Start();
            }

            // Start Data Servers
            foreach (var dataServer in config.DataServers)
            {
                dataServer.Start();
            }

            // Start Device Finder
            if (config.DeviceFinder != null)
            {
                config.DeviceFinder.DeviceFound += DeviceFinder_DeviceFound;
                config.DeviceFinder.SearchCompleted += DeviceFinder_SearchCompleted;
                config.DeviceFinder.Start();
            }
        }

        private void DeviceFinder_SearchCompleted(long milliseconds)
        {
            Configuration.Save();
        }

        private void DeviceFinder_DeviceFound(MTConnectSniffer.MTConnectDevice device)
        {
            AddDevice(device);
        }

        private void AddDevice(MTConnectSniffer.MTConnectDevice device)
        {
            string deviceId = GenerateDeviceId(device);

            if (!Configuration.Devices.Exists(o => o.DeviceId == deviceId))
            {
                string f = "http://{0}:{1}";
                string url = string.Format(f, device.IpAddress, device.Port);

                var d = new Device();
                d.DeviceId = deviceId;
                d.AgentUrl = url;
                d.DeviceName = device.DeviceName;
                Configuration.Devices.Add(d);
                d.Start();

                log.Info("New Device Added : " + deviceId + " : " + device.DeviceName + " : " + url);
            }
        }

        private static string GenerateDeviceId(MTConnectSniffer.MTConnectDevice device)
        {
            // Create Identifier input
            string f = "{0}|{1}|{2}";
            string s = string.Format(f, device.DeviceName, device.Port, device.MacAddress);
            s = Uri.EscapeDataString(s);

            // Create Hash
            var h = System.Security.Cryptography.SHA1.Create();
            var b = Encoding.UTF8.GetBytes(s);
            b = h.ComputeHash(b);
            List<byte> l = b.ToList();
            l.Reverse();
            b = l.ToArray();

            // Convert to Base64 string
            s = Convert.ToBase64String(b);

            // Remove non alphanumeric characters
            var regex = new Regex("[^a-zA-Z0-9 -]");
            s = regex.Replace(s, "");
            s = s.ToUpper();

            return s;
        }

        private void ContainerDefinitionsReceived(List<ContainerDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = ContainerDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) ContainerDefinitions.RemoveAt(i);
                    ContainerDefinitions.Add(definition);
                }
            }
        }

        private void DataDefinitionsReceived(List<DataDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = DataDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) DataDefinitions.RemoveAt(i);
                    DataDefinitions.Add(definition);
                }
            }
        }

        private void DataSamplesReceived(List<DataSample> samples)
        {
            // Update Current Samples
            foreach (var sample in samples)
            {
                lock (_lock)
                {
                    int i = CurrentSamples.FindIndex(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id);
                    if (i >= 0) CurrentSamples.RemoveAt(i);
                    CurrentSamples.Add(sample);
                }
            }

            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(samples);
            }
        }

        private void PrintHeader()
        {
            log.Info("---------------------------");
            log.Info("TrakHound DataClient : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            log.Info("---------------------------");
        }
    }
}
