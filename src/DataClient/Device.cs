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
using TrakHound.Api.v2.Data;
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

        private string agentUrl;


        public ConnectionDefinitionData AgentConnection;

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
                if (AgentConnection == null) AgentConnection = new ConnectionDefinitionData();
                AgentConnection.DeviceId = _deviceId;
            }
        }

        private string _address;
        /// <summary>
        /// Gets the Address for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("address")]
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != null) throw new InvalidOperationException("Cannot set value. Address is ReadOnly!");
                _address = value;
                if (AgentConnection == null) AgentConnection = new ConnectionDefinitionData();
                AgentConnection.Address = _address;
            }
        }

        private string _physicalAddress;
        /// <summary>
        /// Gets the Physical Address for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("physicalAddress")]
        public string PhysicalAddress
        {
            get { return _physicalAddress; }
            set
            {
                if (_physicalAddress != null) throw new InvalidOperationException("Cannot set value. PhysicalAddress is ReadOnly!");
                _physicalAddress = value;
                if (AgentConnection == null) AgentConnection = new ConnectionDefinitionData();
                AgentConnection.PhysicalAddress = _physicalAddress;
            }
        }

        private int _port = -1;
        /// <summary>
        /// Gets the Port for the MTConnect Agent. Read Only.
        /// </summary>
        [XmlAttribute("port")]
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port >= 0) throw new InvalidOperationException("Cannot set value. Port is ReadOnly!");
                _port = value;
                if (AgentConnection == null) AgentConnection = new ConnectionDefinitionData();
                AgentConnection.Port = _port;
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

        public Device(string deviceId, Connection connection, string deviceName)
        {
            Init(deviceId, connection, deviceName, 100);
        }

        public Device(string deviceId, Connection connection, string deviceName, int interval)
        {
            Init(deviceId, connection, deviceName, interval);
        }

        private void Init(string deviceId, Connection connection, string deviceName, int interval)
        {
            if (connection != null)
            {
                AgentConnection = new ConnectionDefinitionData();
                AgentConnection.Address = connection.Address;
                AgentConnection.Port = connection.Port;
                AgentConnection.PhysicalAddress = connection.PhysicalAddress;
                AgentConnection.DeviceId = deviceId;

                DeviceId = deviceId;
                Address = connection.Address;
                PhysicalAddress = connection.PhysicalAddress;
                Port = connection.Port;
                DeviceName = deviceName;
                Interval = interval;
            }
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
            // Create the MTConnect Agent Base URL
            agentUrl = string.Format("http://{0}:{1}", Address, Port);

            // Create a new MTConnectClient using the baseUrl
            _agentClient = new MTConnectClient(agentUrl, DeviceName);
            _agentClient.Interval = Interval;

            // Subscribe to the Event handlers to receive status events
            _agentClient.Started += _agentClient_Started;
            _agentClient.Stopped += _agentClient_Stopped;

            // Subscribe to the Event handlers to receive the MTConnect documents
            _agentClient.ProbeReceived += DevicesSuccessful;
            _agentClient.CurrentReceived += StreamsSuccessful;
            _agentClient.SampleReceived += StreamsSuccessful;

            // Start the MTConnectClient
            _agentClient.Start();
        }

        private void _agentClient_Started()
        {
            log.Info("MTConnect Client Started : " + agentUrl + "/" + DeviceName);
        }

        private void _agentClient_Stopped()
        {
            log.Info("MTConnect Client Stopped : " + agentUrl + "/" + DeviceName);
        }

        private void DevicesSuccessful(MTConnectDevices.Document document)
        {
            log.Trace("MTConnect Devices Document Received @ " + DateTime.Now.ToString("o"));

            if (document.Header != null && document.Devices != null && document.Devices.Count == 1)
            {
                long agentInstanceId = document.Header.InstanceId;
                DateTime timestamp = document.Header.CreationTime;

                // Send Agent Definition
                AgentDefinitionsReceived?.Invoke(Create(DeviceId, document.Header));

                var dataItemDefinitions = new List<DataItemDefinitionData>();
                var componentDefinitions = new List<ComponentDefinitionData>();

                var device = document.Devices[0];

                // Send Device Definition
                DeviceDefinitionsReceived?.Invoke(Create(DeviceId, agentInstanceId, device));

                // Add Path DataItems
                foreach (var item in device.DataItems)
                {
                    dataItemDefinitions.Add(Create(DeviceId, agentInstanceId, device.Id, item));
                }

                // Add Component Definitions
                componentDefinitions.AddRange(GetComponents(DeviceId, agentInstanceId, device.Id, device.Components));

                // Add Component DataItems
                foreach (var component in device.GetComponents())
                {
                    foreach (var dataItem in component.DataItems)
                    {
                        dataItemDefinitions.Add(Create(DeviceId, agentInstanceId, component.Id, dataItem));
                    }
                }

                // Send ContainerDefinition Objects
                if (componentDefinitions.Count > 0) ComponentDefinitionsReceived?.Invoke(componentDefinitions);

                // Send DataItemDefinition Objects
                if (dataItemDefinitions.Count > 0) DataItemDefinitionsReceived?.Invoke(dataItemDefinitions);
            }
        }

        private static List<ComponentDefinitionData> GetComponents(string deviceId, long agentInstanceId, string parentId, MTConnectDevices.ComponentCollection components)
        {
            var l = new List<ComponentDefinitionData>();

            foreach (var component in components.Components)
            {
                l.Add(Create(deviceId, agentInstanceId, parentId, component));
                l.AddRange(GetComponents(deviceId, agentInstanceId, component.Id, component.SubComponents));
            }

            return l;
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
                    samples.Add(Create(DeviceId, document.Header.InstanceId, dataItem));
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

        private static DeviceDefinitionData Create(string deviceId, long agentInstanceId, MTConnectDevices.Device device)
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

        private static ComponentDefinitionData Create(string deviceId, long agentInstanceId, string parentId, MTConnectDevices.Component component)
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

        private static DataItemDefinitionData Create(string deviceId, long agentInstanceId, string parentId, MTConnectDevices.DataItem dataItem)
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

        private static SampleData Create(string deviceId, long agentInstanceId, MTConnectStreams.DataItem dataItem)
        {
            var obj = new SampleData();

            obj.DeviceId = deviceId;

            obj.Id = dataItem.DataItemId;
            obj.AgentInstanceId = agentInstanceId;
            obj.Sequence = dataItem.Sequence;
            obj.Timestamp = dataItem.Timestamp;
            obj.CDATA = dataItem.CDATA;
            if (dataItem.Category == DataItemCategory.CONDITION) obj.Condition = ((MTConnectStreams.Condition)dataItem).ConditionValue.ToString();

            return obj;
        }

    }
}
