// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using MTConnect;
using MTConnect.Clients;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Streams.Data;
using MTConnectDevices = MTConnect.MTConnectDevices;
using MTConnectStreams = MTConnect.MTConnectStreams;

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

        private int _interval = -1;
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
                AgentDefinitionsReceived?.Invoke(Create(DeviceId, document.Header));

                var dataItemDefinitions = new List<DataItemDefinitionData>();
                var componentDefinitions = new List<ComponentDefinitionData>();

                var device = document.Devices[0];

                // Send Device Definition
                DeviceDefinitionsReceived?.Invoke(Create(DeviceId, device, agentInstanceId));

                // Add Path DataItems
                foreach (var item in device.DataItems)
                {
                    dataItemDefinitions.Add(Create(DeviceId, item, device.Id, agentInstanceId));
                }

                // Create a ContainerDefinition for each Component
                foreach (var component in device.GetComponents())
                {
                    // Add Component Container
                    componentDefinitions.Add(Create(DeviceId, component, device.Id, agentInstanceId));

                    // Add DataItems
                    foreach (var dataItem in component.DataItems)
                    {
                        dataItemDefinitions.Add(Create(DeviceId, dataItem, component.Id, agentInstanceId));
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

            if (!document.DeviceStreams.IsNullOrEmpty())
            {
                var samples = new List<SampleData>();

                var deviceStream = document.DeviceStreams[0];

                foreach (var dataItem in deviceStream.DataItems)
                {
                    samples.Add(Create(DeviceId, dataItem));
                }

                if (samples.Count > 0) SamplesReceived?.Invoke(samples);
            }
        }

        private static AgentDefinitionData Create(string deviceId, MTConnect.Headers.MTConnectDevicesHeader header)
        {
            var obj = new AgentDefinitionData();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.Timestamp = header.CreationTime;

            // MTConnect Properties
            obj.InstanceId = header.InstanceId;
            obj.Sender = header.Sender;
            obj.Version = header.Version;
            obj.BufferSize = header.BufferSize;
            obj.TestIndicator = header.TestIndicator;

            return obj;
        }

        private static DeviceDefinitionData Create(string deviceId, MTConnectDevices.Device device, string agentInstanceId)
        {
            var obj = new DeviceDefinitionData();

            obj.DeviceId = deviceId;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Id = device.Id;
            obj.Uuid = device.Uuid;
            obj.Name = device.Name;
            obj.NativeName = device.NativeName;
            obj.SampleInterval = device.SampleInterval;
            obj.SampleRate = device.SampleRate;
            obj.Iso841Class = device.Iso841Class;

            return obj;
        }

        private static ComponentDefinitionData Create(string deviceId, MTConnectDevices.Component component, string parentId, string agentInstanceId)
        {
            var obj = new ComponentDefinitionData();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.ParentId = parentId;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Type = component.Type;
            obj.Id = component.Id;
            obj.Uuid = component.Uuid;
            obj.Name = component.Name;
            obj.NativeName = component.NativeName;
            obj.SampleInterval = component.SampleInterval;
            obj.SampleRate = component.SampleRate;

            return obj;
        }

        private static DataItemDefinitionData Create(string deviceId, MTConnectDevices.DataItem dataItem, string parentId, string agentInstanceId)
        {
            var obj = new DataItemDefinitionData();

            // TrakHound Properties
            obj.DeviceId = deviceId;
            obj.ParentId = parentId;

            // MTConnect Properties
            obj.AgentInstanceId = agentInstanceId;
            obj.Id = dataItem.Id;
            obj.Name = dataItem.Name;
            obj.Category = dataItem.Category.ToString();
            obj.Type = dataItem.Type;
            obj.SubType = dataItem.SubType;
            obj.Statistic = dataItem.Statistic;
            obj.Units = dataItem.Units;
            obj.NativeUnits = dataItem.NativeUnits;
            obj.NativeScale = dataItem.NativeScale;
            obj.CoordinateSystem = dataItem.CoordinateSystem;
            obj.SampleRate = dataItem.SampleRate;
            obj.Representation = dataItem.Representation;
            obj.SignificantDigits = dataItem.SignificantDigits;

            return obj;
        }

        private static SampleData Create(string deviceId, MTConnectStreams.DataItem dataItem)
        {
            var obj = new SampleData();

            obj.DeviceId = deviceId;

            obj.Id = dataItem.DataItemId;
            obj.Sequence = dataItem.Sequence;
            obj.Timestamp = dataItem.Timestamp;
            obj.CDATA = dataItem.CDATA;
            if (dataItem.Category == DataItemCategory.CONDITION) obj.Condition = ((MTConnectStreams.Condition)dataItem).ConditionValue.ToString();

            return obj;
        }
    }
}
