// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;

namespace TrakHound.DataClient
{
    /// <summary>
    /// Handles all functions for collecting data from MTConnect Agents and sending that data to TrakHound DataServers
    /// </summary>
    public class DataClient
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        internal static object _lock = new object();
        private int devicesFound = 0;
        private MTConnectSniffer.MTConnectDevice foundDevice;
        private DeviceStartQueue deviceStartQueue = new DeviceStartQueue();


        private Configuration _configuration;
        /// <summary>
        /// Gets the Configuration that was used to create the DataClient. Read Only.
        /// </summary>
        public Configuration Configuration { get { return _configuration; } }


        public DataClient(Configuration config)
        {
            PrintHeader();

            _configuration = config;

            deviceStartQueue.DeviceStarted += DeviceStartQueue_DeviceStarted;
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
            deviceStartQueue.Start();
            foreach (var device in _configuration.Devices)
            {
                log.Info("Device Read : " + device.DeviceId + " : " + device.DeviceName + " : " + device.Address + " : " + device.Port);
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
            if (deviceStartQueue != null) deviceStartQueue.Stop();
        }

        private void DeviceFinder_SearchCompleted(long milliseconds)
        {
            if (devicesFound > 0)
            {
                Configuration.Save();
            }

            var time = TimeSpan.FromMilliseconds(milliseconds);
            log.Info(string.Format("Device Finder : Search Completed : {0} Devices Found in {1}", devicesFound, time.ToString()));

            devicesFound = 0;
            foundDevice = null;
        }

        private void DeviceFinder_DeviceFound(MTConnectSniffer.MTConnectDevice device)
        {
            foundDevice = device;
            if (AddDevice(device)) devicesFound++;        
        }

        private bool AddDevice(MTConnectSniffer.MTConnectDevice device)
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

                return true;
            }

            return false;
        }

        private void StartDevice(Device device)
        {
            device.AgentDefinitionsReceived += AgentDefinitionReceived;
            device.DeviceDefinitionsReceived += DeviceDefinitionReceived;
            device.ComponentDefinitionsReceived += ComponentDefinitionsReceived;
            device.DataItemDefinitionsReceived += DataDefinitionsReceived;
            device.SamplesReceived += SamplesReceived;
            device.StatusUpdated += StatusUpdated;

            // Add to Start Queue (to prevent all Devices from starting at once and using too many resources)
            deviceStartQueue.Add(device);

            // Send Connection to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(device.AgentConnection);
            }
        }

        private void DeviceStartQueue_DeviceStarted(Device device)
        {
            log.Info("Device Started : " + device.DeviceId + " : " + device.DeviceName + " : " + device.Address + " : " + device.Port);
        }

        private void AgentDefinitionReceived(AgentDefinitionData definition)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void DeviceDefinitionReceived(DeviceDefinitionData definition)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definition);
            }
        }

        private void ComponentDefinitionsReceived(List<ComponentDefinitionData> definitions)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }

        private void DataDefinitionsReceived(List<DataItemDefinitionData> definitions)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(definitions.ToList<IStreamData>());
            }
        }
   
        private void SamplesReceived(List<SampleData> samples)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(samples.ToList<IStreamData>());
            }
        }

        private void StatusUpdated(StatusData status)
        {
            // Send to DataServers
            foreach (var dataServer in Configuration.DataServers)
            {
                dataServer.Add(status);
            }
        }


        private static void PrintHeader()
        {
            log.Info("---------------------------");
            log.Info("TrakHound DataClient : v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info(@"Copyright 2017 TrakHound Inc., All Rights Reserved");
            log.Info("---------------------------");
        }

        public static string GenerateDeviceId(MTConnectSniffer.MTConnectDevice device)
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
