// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MTConnect.Clients;
using NLog;
using System.Collections.Generic;
using System.Xml.Serialization;
using MTConnectDevices = MTConnect.MTConnectDevices;
using MTConnectStreams = MTConnect.MTConnectStreams;
using TrakHound.DataClient.Data;

namespace TrakHound.DataClient
{
    public class Device
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private MTConnectClient agentClient;

        [XmlAttribute("deviceId")]
        public string DeviceId { get; set; }

        [XmlText]
        public string AgentUrl { get; set; }

        [XmlAttribute("deviceName")]
        public string DeviceName { get; set; }

        public event AgentDefinitionsHandler AgentDefinitionsReceived;
        public event ComponentDefinitionsHandler ComponentDefinitionsReceived;
        public event DataItemDefinitionsHandler DataItemDefinitionsReceived;
        public event DeviceDefinitionsHandler DeviceDefinitionsReceived;
        public event SamplesHandler SamplesReceived;


        public void Start()
        {
            StartAgentClient();
        }

        public void Stop()
        {
            if (agentClient != null) agentClient.Stop();
        }

        private void StartAgentClient()
        {
            // Create a new MTConnectClient using the baseUrl
            agentClient = new MTConnectClient(AgentUrl, DeviceName);
            agentClient.Interval = 500;

            // Subscribe to the Event handlers to receive the MTConnect documents
            agentClient.ProbeReceived += DevicesSuccessful;
            agentClient.CurrentReceived += StreamsSuccessful;
            agentClient.SampleReceived += StreamsSuccessful;

            // Start the MTConnectClient
            agentClient.Start();
        }

        void DevicesSuccessful(MTConnectDevices.Document document)
        {
            if (document.Header != null)
            {
                // Send Agent Definition
                AgentDefinitionsReceived?.Invoke(new AgentDefinition(DeviceId, document.Header));
            }

            if (document.Devices != null && document.Devices.Count == 1)
            {
                var dataItemDefinitions = new List<DataItemDefinition>();
                var componentDefinitions = new List<ComponentDefinition>();

                var device = document.Devices[0];

                // Send Device Definition
                DeviceDefinitionsReceived?.Invoke(new DeviceDefinition(DeviceId, device));

                // Add Path DataItems
                foreach (var item in device.DataItems)
                {
                    dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, device.Id));
                }

                // Create a ContainerDefinition for each Component
                foreach (var component in device.Components)
                {
                    // Add Component Container
                    componentDefinitions.Add(new ComponentDefinition(DeviceId, component, null));

                    // Add Path DataItems
                    foreach (var item in component.DataItems)
                    {
                        dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, component.Id));
                    }

                    // Process Axes Component
                    if (component.GetType() == typeof(MTConnectDevices.Components.Axes))
                    {
                        var axes = (MTConnectDevices.Components.Axes)component;
                        foreach (var axis in axes.Components)
                        {
                            // Add Axis Component
                            componentDefinitions.Add(new ComponentDefinition(DeviceId, axis, component.Id));

                            // Add Path DataItems
                            foreach (var item in axis.DataItems)
                            {
                                dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, axis.Id));
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
                            componentDefinitions.Add(new ComponentDefinition(DeviceId, path, component.Id));

                            // Add Path DataItems
                            foreach (var item in path.DataItems)
                            {
                                dataItemDefinitions.Add(new DataItemDefinition(DeviceId, item, path.Id));
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

        void StreamsSuccessful(MTConnectStreams.Document document)
        {
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
