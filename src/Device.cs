// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect.Clients;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MTConnectDevices = MTConnect.MTConnectDevices;
using MTConnectStreams = MTConnect.MTConnectStreams;
using TrakHound.DataClient.Data;

namespace TrakHound.DataClient
{
    /// <summary>
    /// Handles MTConnect Agent connection data streams
    /// </summary>
    public class Device
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private MTConnectClient _agentClient;
        /// <summary>
        /// Gets the underlying MTConnectClient object. Read Only.
        /// </summary>
        [XmlIgnore]
        public MTConnectClient AgentClient { get { return _agentClient; } }

        private string _deviceId;
        /// <summary>
        /// Gets the Device ID. Read Only.
        /// </summary>
        [XmlAttribute("deviceId")]
        public string DeviceId
        {
            get { return _deviceId; }
            set
            {
                if (_deviceId != null) throw new InvalidOperationException("Cannot set value. DeviceId is ReadOnly!");
                _deviceId = value;
            }
        }

        private string _agentUrl;
        /// <summary>
        /// Gets the Url for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlText]
        public string AgentUrl
        {
            get { return _agentUrl; }
            set
            {
                if (_agentUrl != null) throw new InvalidOperationException("Cannot set value. AgentUrl is ReadOnly!");
                _agentUrl = value;
            }
        }

        private string _deviceName;
        /// <summary>
        /// Gets the Name of the MTConnect Device. Read Only.
        /// </summary>
        [XmlAttribute("deviceName")]
        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                if (_deviceName != null) throw new InvalidOperationException("Cannot set value. DeviceName is ReadOnly!");
                _deviceName = value;
            }
        }

        private int _interval;
        /// <summary>
        /// Gets the Name of the MTConnect Device. Read Only.
        /// </summary>
        [XmlAttribute("interval")]
        public int Interval
        {
            get { return _interval; }
            set
            {
                if (_interval >= 0) throw new InvalidOperationException("Cannot set value. Interval is ReadOnly!");
                if (value < 0) throw new ArgumentOutOfRangeException("Interval", "Interval must be greater than zero!");
                _interval = value;
            }
        }

        /// <summary>
        /// Event raised when a new AgentDefinition is read.
        /// </summary>
        public event AgentDefinitionsHandler AgentDefinitionsReceived;

        /// <summary>
        /// Event raised when a new DeviceDefinition is read.
        /// </summary>
        public event DeviceDefinitionsHandler DeviceDefinitionsReceived;

        /// <summary>
        /// Event raised when new ComponentDefinitions are read.
        /// </summary>
        public event ComponentDefinitionsHandler ComponentDefinitionsReceived;

        /// <summary>
        /// Event raised when new DataItemDefinitions are read.
        /// </summary>
        public event DataItemDefinitionsHandler DataItemDefinitionsReceived;

        /// <summary>
        /// Event raised when new AgentDefinitions are read.
        /// </summary>
        public event SamplesHandler SamplesReceived;


        public Device() { }

        public Device(string deviceId, string agentUrl, string deviceName)
        {
            Init(deviceId, agentUrl, deviceName, 100);
        }

        public Device(string deviceId, string agentUrl, string deviceName, int interval)
        {
            Init(deviceId, agentUrl, deviceName, interval);
        }

        private void Init(string deviceId, string agentUrl, string deviceName, int interval)
        {
            DeviceId = deviceId;
            AgentUrl = agentUrl;
            DeviceName = deviceName;
            Interval = interval;
        }

        /// <summary>
        /// Start the Device and begin reading the MTConnect Data.
        /// </summary>
        public void Start()
        {
            StartAgentClient();
        }

        /// <summary>
        /// Stop the Device
        /// </summary>
        public void Stop()
        {
            if (_agentClient != null) _agentClient.Stop();
        }

        private void StartAgentClient()
        {
            // Create a new MTConnectClient using the baseUrl
            _agentClient = new MTConnectClient(AgentUrl, DeviceName);
            _agentClient.Interval = Interval;

            // Subscribe to the Event handlers to receive the MTConnect documents
            _agentClient.ProbeReceived += DevicesSuccessful;
            _agentClient.CurrentReceived += StreamsSuccessful;
            _agentClient.SampleReceived += StreamsSuccessful;

            // Start the MTConnectClient
            _agentClient.Start();
        }

        private void DevicesSuccessful(MTConnectDevices.Document document)
        {
            log.Trace("MTConnect Devices Document Received @ " + DateTime.Now.ToString("o"));

            if (document.Header != null && document.Devices != null && document.Devices.Count == 1)
            {
                string agentInstanceId = document.Header.InstanceId.ToString();
                DateTime timestamp = document.Header.CreationTime;

                // Send Agent Definition
                AgentDefinitionsReceived?.Invoke(new AgentDefinition(DeviceId, document.Header));

                var dataItemDefinitions = new List<DataItemDefinition>();
                var componentDefinitions = new List<ComponentDefinition>();

                var device = document.Devices[0];

                // Send Device Definition
                DeviceDefinitionsReceived?.Invoke(new DeviceDefinition(DeviceId, device, agentInstanceId, timestamp));

                // Add Path DataItems
                foreach (var item in device.DataItems)
                {
                    dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, device.Id, agentInstanceId, timestamp));
                }

                // Create a ContainerDefinition for each Component
                foreach (var component in device.Components)
                {
                    // Add Component Container
                    componentDefinitions.Add(new ComponentDefinition(DeviceId, component, device.Id, agentInstanceId, timestamp));

                    // Add Path DataItems
                    foreach (var item in component.DataItems)
                    {
                        dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, component.Id, agentInstanceId, timestamp));
                    }

                    // Process Axes Component
                    if (component.GetType() == typeof(MTConnectDevices.Components.Axes))
                    {
                        var axes = (MTConnectDevices.Components.Axes)component;
                        foreach (var axis in axes.Components)
                        {
                            // Add Axis Component
                            componentDefinitions.Add(new ComponentDefinition(DeviceId, axis, component.Id, agentInstanceId, timestamp));

                            // Add Path DataItems
                            foreach (var item in axis.DataItems)
                            {
                                dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, axis.Id, agentInstanceId, timestamp));
                            }
                        }
                    }

                    // Process Controller Component
                    if (component.GetType() == typeof(MTConnectDevices.Components.Controller))
                    {
                        var controller = (MTConnectDevices.Components.Controller)component;
                        foreach (var path in controller.Components)
                        {
                            // Add Path Component
                            componentDefinitions.Add(new ComponentDefinition(DeviceId, path, component.Id, agentInstanceId, timestamp));

                            // Add Path DataItems
                            foreach (var item in path.DataItems)
                            {
                                dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, path.Id, agentInstanceId, timestamp));
                            }
                        }
                    }
                }

                // Send ContainerDefinition Objects
                if (componentDefinitions.Count > 0) ComponentDefinitionsReceived?.Invoke(componentDefinitions);

                // Send DataItemDefinition Objects
                if (dataItemDefinitions.Count > 0) DataItemDefinitionsReceived?.Invoke(dataItemDefinitions);
            }
        }

        private void StreamsSuccessful(MTConnectStreams.Document document)
        {
            log.Trace("MTConnect Streams Document Received @ " + DateTime.Now.ToString("o"));

            if (document.DeviceStreams != null && document.DeviceStreams.Count > 0)
            {
                var samples = new List<Data.Sample>();

                var deviceStream = document.DeviceStreams[0];

                foreach (var dataItem in deviceStream.DataItems)
                {
                    samples.Add(new Data.Sample(DeviceId, dataItem));
                }

                if (samples.Count > 0) SamplesReceived?.Invoke(samples);
            }
        }
    }
}
