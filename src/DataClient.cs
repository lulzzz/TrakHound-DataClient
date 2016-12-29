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
using TrakHound.Api.v2.Streams;

using TrakHound.DataClient.Data;

namespace TrakHound.DataClient
{
    public class DataClient
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();

        public static List<AgentDefinition> AgentDefinitions = new List<AgentDefinition>();
        public static List<ComponentDefinition> ComponentDefinitions = new List<ComponentDefinition>();
        public static List<DataItemDefinition> DataItemDefinitions = new List<DataItemDefinition>();
        public static List<DeviceDefinition> DeviceDefinitions = new List<DeviceDefinition>();


        public static List<Sample> CurrentSamples = new List<Sample>();


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
                device.AgentDefinitionsReceived += AgentDefinitionReceived;
                device.ComponentDefinitionsReceived += ContainerDefinitionsReceived;
                device.DataItemDefinitionsReceived += DataDefinitionsReceived;
                device.DeviceDefinitionsReceived += DeviceDefinitionReceived;
                device.SamplesReceived += SamplesReceived;
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

        private void AgentDefinitionReceived(AgentDefinition definition)
        {
            lock (_lock)
            {
                int i = AgentDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.InstanceId == definition.InstanceId);
                if (i >= 0) AgentDefinitions.RemoveAt(i);
                AgentDefinitions.Add(definition);
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void ContainerDefinitionsReceived(List<ComponentDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = ComponentDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) ComponentDefinitions.RemoveAt(i);
                    ComponentDefinitions.Add(definition);
                }
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }

        private void DataDefinitionsReceived(List<DataItemDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = DataItemDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) DataItemDefinitions.RemoveAt(i);
                    DataItemDefinitions.Add(definition);
                }
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }

        private void DeviceDefinitionReceived(DeviceDefinition definition)
        {
            lock (_lock)
            {
                int i = DeviceDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                if (i >= 0) DeviceDefinitions.RemoveAt(i);
                DeviceDefinitions.Add(definition);
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void SamplesReceived(List<Sample> samples)
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

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(samples.ToList<IStreamData>());
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
