// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;
using Messaging = TrakHound.Api.v2.Messaging;

namespace TrakHound.DataClient
{
    /// <summary>
    /// Handles all functions for collecting data from MTConnect Agents and sending that data to TrakHound DataServers
    /// </summary>
    public class DataClient
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();
        private int devicesFound = 0;
        private MTConnectSniffer.MTConnectDevice foundDevice;


        private static List<ConnectionDefinitionData> _connectionDefinitions = new List<ConnectionDefinitionData>();
        /// <summary>
        /// Gets a list of current ConnectionDefinitions that have been read. Read Only.
        /// </summary>
        public static ReadOnlyCollection<ConnectionDefinitionData> ConnectionDefinitions { get { return _connectionDefinitions.AsReadOnly(); } }

        private static List<AgentDefinitionData> _agentDefinitions = new List<AgentDefinitionData>();
        /// <summary>
        /// Gets a list of current AgentDefinitions that have been read. Read Only.
        /// </summary>
        public static ReadOnlyCollection<AgentDefinitionData> AgentDefinitions { get { return _agentDefinitions.AsReadOnly(); } }

        private static List<DeviceDefinitionData> _deviceDefinitions = new List<DeviceDefinitionData>();
        /// <summary>
        /// Gets a list of current DeviceDefinitions that have been read. Read Only.
        /// </summary>
        public static ReadOnlyCollection<DeviceDefinitionData> DeviceDefinitions { get { return _deviceDefinitions.AsReadOnly(); } }

        private static List<ComponentDefinitionData> _componentDefinitions = new List<ComponentDefinitionData>();
        /// <summary>
        /// Gets a list of current ComponentDefinitions that have been read. Read Only.
        /// </summary>
        public static ReadOnlyCollection<ComponentDefinitionData> ComponentDefinitions { get { return _componentDefinitions.AsReadOnly(); } }

        private static List<DataItemDefinitionData> _dataItemDefinitions = new List<DataItemDefinitionData>();
        /// <summary>
        /// Gets a list of current DataItemDefinitions that have been read. Read Only.
        /// </summary>
        public static ReadOnlyCollection<DataItemDefinitionData> DataItemDefinitions { get { return _dataItemDefinitions.AsReadOnly(); } }

        private static List<SampleData> _samples = new List<SampleData>();
        /// <summary>
        /// Gets a list of current Samples that have been read. Similar to the MTConnect Current request. Read Only.
        /// </summary>
        public static ReadOnlyCollection<SampleData> Samples { get { return _samples.AsReadOnly(); } }


        private Configuration _configuration;
        /// <summary>
        /// Gets the Configuration that was used to create the DataClient. Read Only.
        /// </summary>
        public Configuration Configuration { get { return _configuration; } }


        public DataClient(Configuration config)
        {
            PrintHeader();

            _configuration = config;
        }

        public void Start()
        {
            log.Info("---------------------------");

            // Start Data Servers
            foreach (var dataServer in _configuration.DataServers)
            {
                dataServer.Start();
                log.Info("DataServer Started : " + dataServer.Name + " @ " + dataServer.Hostname);
            }

            // Start Devices
            foreach (var device in _configuration.Devices)
            {
                StartDevice(device);
            }

            log.Info("---------------------------");

            // Start Device Finder
            if (_configuration.DeviceFinder != null)
            {
                _configuration.DeviceFinder.DeviceFound += DeviceFinder_DeviceFound;
                _configuration.DeviceFinder.SearchCompleted += DeviceFinder_SearchCompleted;
                _configuration.DeviceFinder.Start();

                if (_configuration.DeviceFinder.ScanInterval > 0)
                {
                    var interval = TimeSpan.FromMilliseconds(_configuration.DeviceFinder.ScanInterval);
                    log.Info("Device Finder (Scan Interval = " + interval.ToString() + ") Started..");
                }
                else log.Info("Device Finder Started..");

                log.Info("---------------------------");
            }

            if (_configuration.SendMessages)
            {
                Messaging.Message.Send("trakhound-dataclient-menu", "Notify", "Started");
                Messaging.Message.Send("trakhound-dataclient-menu", "Status", "Running");
            }
        }

        public void Stop()
        {
            log.Info("TrakHound DataClient Stopping..");

            // Stop Devices
            foreach (var device in _configuration.Devices) device.Stop();

            // Stop DataServers
            foreach (var dataServer in _configuration.DataServers) dataServer.Stop();

            // Stop the Device Finder
            var deviceFinder = _configuration.DeviceFinder;
            if (deviceFinder != null) deviceFinder.Stop();


            if (_configuration.SendMessages)
            {
                Messaging.Message.Send("trakhound-dataclient-menu", "Notify", "Stopped");
                Messaging.Message.Send("trakhound-dataclient-menu", "Status", "Stopped");
            }
        }

        private void DeviceFinder_SearchCompleted(long milliseconds)
        {
            if (_configuration.SendMessages)
            {
                if (devicesFound > 1)
                {
                    var text = string.Format("{0} New Devices Found", devicesFound);
                    Messaging.Message.Send("trakhound-dataclient-menu", "Notify", text);
                }
                else if (devicesFound > 0 && foundDevice != null)
                {
                    var text = string.Format("New Device Found ({0} @ {1}:{2})", foundDevice.DeviceName, foundDevice.IpAddress, foundDevice.Port);
                    Messaging.Message.Send("trakhound-dataclient-menu", "Notify", text);
                }
            }

            devicesFound = 0;
            foundDevice = null;

            Configuration.Save();
        }

        private void DeviceFinder_DeviceFound(MTConnectSniffer.MTConnectDevice device)
        {
            foundDevice = device;
            devicesFound++;
            AddDevice(device);
        }

        private void AddDevice(MTConnectSniffer.MTConnectDevice device)
        {
            // Generate the Device ID Hash
            string deviceId = GenerateDeviceId(device);

            // Check to make sure the Device is not already added
            if (!Configuration.Devices.Exists(o => o.DeviceId == deviceId))
            {
                var conn = new Api.v2.Data.Connection();
                conn.Address = device.IpAddress.ToString();
                conn.PhysicalAddress = device.MacAddress.ToString();
                conn.Port = device.Port;

                // Create a new Device and start it
                var d = new Device(deviceId, conn, device.DeviceName);
                Configuration.Devices.Add(d);
                StartDevice(d);

                log.Info("New Device Added : " + deviceId + " : " + device.DeviceName + " : " + device.IpAddress + " : " + device.Port);
            }
        }

        private void StartDevice(Device device)
        {
            device.AgentDefinitionsReceived += AgentDefinitionReceived;
            device.DeviceDefinitionsReceived += DeviceDefinitionReceived;
            device.ComponentDefinitionsReceived += ComponentDefinitionsReceived;
            device.DataItemDefinitionsReceived += DataDefinitionsReceived;
            device.SamplesReceived += SamplesReceived;
            device.Start();

            // Send Connection to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(device.AgentConnection);
            }

            log.Info("Device Started : " + device.DeviceId + " : " + device.DeviceName + " : " + device.Address + " : " + device.Port);
        }

        private void AgentDefinitionReceived(AgentDefinitionData definition)
        {
            lock (_lock)
            {
                int i = _agentDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.InstanceId == definition.InstanceId);
                if (i >= 0) _agentDefinitions.RemoveAt(i);
                _agentDefinitions.Add(definition);
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void DeviceDefinitionReceived(DeviceDefinitionData definition)
        {
            lock (_lock)
            {
                int i = _deviceDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                if (i >= 0) _deviceDefinitions.RemoveAt(i);
                _deviceDefinitions.Add(definition);
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void ComponentDefinitionsReceived(List<ComponentDefinitionData> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = _componentDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) _componentDefinitions.RemoveAt(i);
                    _componentDefinitions.Add(definition);
                }
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }

        private void DataDefinitionsReceived(List<DataItemDefinitionData> definitions)
        {
            foreach (var definition in definitions)
            {
                lock (_lock)
                {
                    int i = _dataItemDefinitions.FindIndex(o => o.DeviceId == definition.DeviceId && o.Id == definition.Id);
                    if (i >= 0) _dataItemDefinitions.RemoveAt(i);
                    _dataItemDefinitions.Add(definition);
                }
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }
   
        private void SamplesReceived(List<SampleData> samples)
        {
            // Update Current Samples
            foreach (var sample in samples)
            {
                lock (_lock)
                {
                    int i = _samples.FindIndex(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id);
                    if (i >= 0) _samples.RemoveAt(i);
                    _samples.Add(sample);
                }
            }

            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(samples.ToList<IStreamData>());
            }
        }


        private static void PrintHeader()
        {
            log.Info("---------------------------");
            log.Info("TrakHound DataClient : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            log.Info("---------------------------");
        }

        private static string GenerateDeviceId(MTConnectSniffer.MTConnectDevice device)
        {
            // Create Identifier input
            string s = string.Format("{0}|{1}|{2}", device.DeviceName, device.Port, device.MacAddress);
            s = Uri.EscapeDataString(s);

            // Create Hash
            var b = Encoding.UTF8.GetBytes(s);
            var h = SHA1.Create();
            b = h.ComputeHash(b);
            var l = b.ToList();
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
    }
}
