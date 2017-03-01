// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using TrakHound.Api.v2;
using TrakHound.Api.v2.Streams;
using TrakHound.Api.v2.Streams.Data;
using TrakHound.DataClient.DataGroups;

namespace TrakHound.DataClient
{
    /// <summary>
    /// Handles all of the functions for sending data to a TrakHound DataServer application
    /// </summary>
    public class DataServer
    {
        /// <summary>
        /// The maximum number of items to send to a DataServer at one time
        /// </summary>
        private const int MAX_SEND_COUNT = 2000;

        /// <summary>
        /// The interval (in milliseconds) that the Buffer is read
        /// </summary>
        private const int BUFFER_READ_INTERVAL = 5000;

        /// <summary>
        /// The maximum number of items to read from the Buffer at one time
        /// </summary>
        private const int MAX_BUFFER_READ_COUNT = 5000;

        private static Logger log = LogManager.GetCurrentClassLogger();

        private object _lock = new object();
        private ManualResetEvent sendStop;
        private Thread bufferThread;
        private StreamClient streamClient;
        private bool connected;

        private static List<ComponentDefinitionData> components = new List<ComponentDefinitionData>();
        private static List<DataItemDefinitionData> dataItems = new List<DataItemDefinitionData>();
        private static List<SampleData> currentSamples = new List<SampleData>();


        /// <summary>
        /// List of Configured DataGroups for processing data
        /// </summary>
        [XmlArray("DataGroups")]
        [XmlArrayItem("DataGroup")]
        public List<DataGroup> DataGroups { get; set; }

        /// <summary>
        /// Gets or Sets the name of the DataServer
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        private string _hostname;
        /// <summary>
        /// Gets or Sets the Hostname of the DataServer
        /// </summary>
        [XmlAttribute("hostname")]
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                _hostname = value;

                if (Buffer != null) _buffer._hostname = _hostname;
            }
        }

        /// <summary>
        /// Gets or Sets the port used to stream data to DataServer
        /// </summary>
        [XmlAttribute("port")]
        public int Port { get; set; }

        /// <summary>
        /// Gets or Sets whether to use SSL when connecting to the DataServer
        /// </summary>
        [XmlAttribute("useSSL")]
        public bool UseSSL { get; set; }

        /// <summary>
        /// Gets or Sets the interval at which data is sent to the DataServer
        /// </summary>
        [XmlAttribute("sendInterval")]
        public int SendInterval { get; set; }

        private Buffer _buffer;
        /// <summary>
        /// Gets or Sets the Buffer to use for buffering data between connection interruptions
        /// </summary>
        [XmlElement("Buffer")]
        public Buffer Buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                if (_buffer != null) _buffer._hostname = Hostname;
            }
        }

        /// <summary>
        /// Gets or Sets the API Key used to send data to the TrakHound Cloud
        /// </summary>
        [XmlAttribute("apiKey")]
        public string ApiKey { get; set; }

        public DataServer()
        {
            SendInterval = 500;
            Port = 8472;
        }

        /// <summary>
        /// Start the DataServer streaming
        /// </summary>
        public void Start()
        {
            sendStop = new ManualResetEvent(false);

            streamClient = new StreamClient(Hostname, Port, UseSSL);
            streamClient.SendFailed += StreamClient_SendFailed;
            streamClient.SendSuccessful += StreamClient_SendSuccessful;
            streamClient.Connected += StreamClient_Connected;
            streamClient.Disconnected += StreamClient_Disconnected;
            streamClient.Start();

            // Start Buffer Thread if the Buffer is configured
            if (Buffer != null)
            {
                Buffer.Start(Hostname);

                bufferThread = new Thread(new ThreadStart(BufferWorker));
                bufferThread.Start();
            }
        }


        /// <summary>
        /// Stop the DataServer
        /// </summary>
        public void Stop()
        {
            if (sendStop != null) sendStop.Set();

            if (streamClient != null) streamClient.Close();

            if (Buffer != null) Buffer.Stop();

            log.Info("DataServer : " + Hostname + " Stopped");
        }

        /// <summary>
        /// Send a single item to the DataServer
        /// </summary>
        public void Add(IStreamData data)
        {
            Add(new List<IStreamData>() { data });
        }

        /// <summary>
        /// Send a list of items to the DataServer
        /// </summary>
        public void Add(List<IStreamData> data)
        {
            var added = new List<IStreamData>();

            // Add Components to stored list
            foreach (var component in data.OfType<ComponentDefinitionData>().ToList())
            {
                lock(_lock)
                {
                    int i = components.FindIndex(o => o.DeviceId == component.DeviceId && o.Id == component.Id);
                    if (i >= 0) components.RemoveAt(i);
                    components.Add(component);
                }
            }

            // Add DataItems to stored list
            foreach (var dataItem in data.OfType<DataItemDefinitionData>().ToList())
            {
                lock (_lock)
                {
                    int i = dataItems.FindIndex(o => o.DeviceId == dataItem.DeviceId && o.Id == dataItem.Id);
                    if (i >= 0) dataItems.RemoveAt(i);
                    dataItems.Add(dataItem);
                }
            }

            // Add Samples to stored list
            foreach (var sample in data.OfType<SampleData>().ToList())
            {
                lock (_lock)
                {
                    int i = currentSamples.FindIndex(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id);
                    if (i >= 0) currentSamples.RemoveAt(i);
                    currentSamples.Add(sample);
                }
            }

            // Add Statuses
            var statusData = data.OfType<StatusData>().ToList();
            if (statusData != null && statusData.Count > 0)
            {
                var statuses = new List<StatusData>();

                var deviceIds = statusData.Select(o => o.DeviceId).Distinct();
                foreach (var deviceId in deviceIds)
                {
                    statuses.Add(statusData.FindAll(o => o.DeviceId == deviceId).OrderByDescending(o => o.Timestamp).First());
                }

                added.AddRange(statuses);
            }

            // Add any Definitions
            added.AddRange(data.OfType<ConnectionDefinitionData>().ToList());
            added.AddRange(data.OfType<AgentDefinitionData>().ToList());
            added.AddRange(data.OfType<ComponentDefinitionData>().ToList());
            added.AddRange(data.OfType<DataItemDefinitionData>().ToList());
            added.AddRange(data.OfType<DeviceDefinitionData>().ToList());

            // Add Samples using the configured Filters
            var samples = data.OfType<SampleData>().ToList();
            if (!samples.IsNullOrEmpty())
            {
                var sampleSendList = new List<SampleData>();

                // Add Archive DataGroups
                foreach (var sample in FilterSamples(samples, CaptureMode.ARCHIVE))
                {
                    sampleSendList.Add(sample);
                    log.Trace(sample.StreamDataType.ToString() + " : " + sample.Id + " : " + sample.Timestamp + " : " + sample.CDATA + " : " + sample.Condition);
                }

                // Add Current DataGroups
                foreach (var sample in FilterSamples(samples, CaptureMode.CURRENT))
                {
                    if (!sampleSendList.Exists(o => o.Id == sample.Id))
                    {
                        sampleSendList.Add(sample);
                        log.Trace("CURRENT : " + sample.Id + " : " + sample.Timestamp + " : " + sample.CDATA + " : " + sample.Condition);
                    }
                }

                added.AddRange(sampleSendList);
            }

            if (added.Count > 0)
            {
                var sendList = new List<IStreamData>();

                if (Buffer != null)
                {
                    // Get the max amount of items to send at one time
                    sendList.AddRange(added.Take(MAX_SEND_COUNT).ToList());

                    // Add the rest to the Buffer
                    if (added.Count > MAX_SEND_COUNT)
                    {
                        var bufferList = added.GetRange(MAX_SEND_COUNT, added.Count - MAX_SEND_COUNT);
                        bufferList = bufferList.FindAll(o => o.StreamDataType != StreamDataType.CURRENT_SAMPLE);
                        if (bufferList.Count > 0)
                        {
                            Buffer.Add(bufferList);
                            log.Info(Hostname + " : " + bufferList.Count + " Added to Buffer. Exceeded Max Send Count.");
                        }
                    }
                }
                else
                {
                    sendList.AddRange(added);
                    if (added.Count > MAX_SEND_COUNT)
                    {
                        log.Warn(Hostname + " : " + (added.Count - MAX_SEND_COUNT) + " Added to Buffer. Exceeded Max Send Count. Configure a Buffer to not lose data!");
                    }
                }

                // Add the Api Key
                if (!string.IsNullOrEmpty(ApiKey))
                {
                    foreach (var item in sendList) item.ApiKey = ApiKey;
                }

                // Send filtered Samples
                streamClient.Write(sendList);
            }
        }

        private List<SampleData> FilterSamples(List<SampleData> samples, CaptureMode captureMode)
        {
            var l = new List<SampleData>();

            var _dataItems = dataItems.ToList();
            var _components = components.ToList();

            foreach (var dataGroup in DataGroups.FindAll(o => o.CaptureMode == captureMode))
            {
                var filtered = samples.FindAll(o => dataGroup.CheckFilters(o, _dataItems, _components));
                foreach (var s in filtered)
                {
                    var sample = s.Copy();

                    // Set the StreamDataType
                    if (captureMode == CaptureMode.ARCHIVE) sample.SetStreamDataType(StreamDataType.ARCHIVED_SAMPLE);
                    else sample.SetStreamDataType(StreamDataType.CURRENT_SAMPLE);

                    // Add to list if new
                    if (!l.Exists(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id && o.Timestamp >= sample.Timestamp))
                    {
                        l.Add(sample);
                    }

                    // Include other DataGroups
                    foreach (var includedGroup in dataGroup.IncludedDataGroups)
                    {
                        // Find group by name
                        var group = DataGroups.Find(o => o.Name == includedGroup);
                        if (group != null)
                        {
                            // Find most current samples for the group's filters
                            var currentFiltered = currentSamples.FindAll(o => group.CheckFilters(o, _dataItems, _components));
                            foreach (var s1 in currentFiltered)
                            {
                                var currentSample = s.Copy();

                                // Set the StreamDataType
                                if (captureMode == CaptureMode.ARCHIVE) currentSample.SetStreamDataType(StreamDataType.ARCHIVED_SAMPLE);
                                else currentSample.SetStreamDataType(StreamDataType.CURRENT_SAMPLE);

                                // Add to list if new
                                if (!l.Exists(o => o.DeviceId == currentSample.DeviceId && o.Id == currentSample.Id && o.Timestamp >= currentSample.Timestamp))
                                {
                                    l.Add(currentSample);
                                }
                            }
                        }
                    }
                }
            }

            return l;
        }

        private void BufferWorker()
        {
            do
            {
                if (connected)
                {
                    int maxRecords = MAX_BUFFER_READ_COUNT;

                    var sendList = new List<IStreamData>();

                    sendList.AddRange(Buffer.Read<ConnectionDefinitionData>(maxRecords - sendList.Count).ToList<IStreamData>());
                    sendList.AddRange(Buffer.Read<AgentDefinitionData>(maxRecords - sendList.Count).ToList<IStreamData>());
                    sendList.AddRange(Buffer.Read<ComponentDefinitionData>(maxRecords - sendList.Count).ToList<IStreamData>());
                    sendList.AddRange(Buffer.Read<DataItemDefinitionData>(maxRecords - sendList.Count).ToList<IStreamData>());
                    sendList.AddRange(Buffer.Read<DeviceDefinitionData>(maxRecords - sendList.Count).ToList<IStreamData>());
                    sendList.AddRange(Buffer.Read<SampleData>(maxRecords - sendList.Count).ToList<IStreamData>());

                    if (sendList.Count > 0)
                    {
                        var ids = sendList.Select(o => o.EntryId).ToList();

                        log.Info(Hostname + " : " + sendList.Count + " Samples Read from Buffer");

                        // Send Samples to Data Server
                        streamClient.Write(sendList);
                        Buffer.Remove(ids);
                    }
                }              
            } while (!sendStop.WaitOne(BUFFER_READ_INTERVAL, true));
        }

        private void StreamClient_SendSuccessful(int successfulCount)
        {
            log.Info(Hostname + " : " + successfulCount + " Items Sent Successfully");
        }

        private void StreamClient_SendFailed(List<IStreamData> streamData)
        {
            if (Buffer != null)
            {
                // Don't buffer Current Samples or Statuses
                var bufferItems = streamData.FindAll(o => o.StreamDataType != StreamDataType.CURRENT_SAMPLE && o.StreamDataType != StreamDataType.STATUS);
                var failedItems = streamData.FindAll(o => o.StreamDataType == StreamDataType.CURRENT_SAMPLE || o.StreamDataType == StreamDataType.STATUS);
                if (bufferItems.Count > 0)
                {
                    Buffer.Add(bufferItems);
                    log.Warn(string.Format("{0} : {1} Falied to Send. {2} Added to Buffer.", Hostname, bufferItems.Count + failedItems.Count, bufferItems.Count));
                }
                else if (failedItems.Count > 0)
                {
                    log.Warn(Hostname + " : " + failedItems.Count + " Failed to Send.");
                }       
            }
            else
            {
                log.Warn(Hostname + " : " + streamData.Count + " Failed to Send.");
            }
        }

        private void StreamClient_Connected(object sender, System.EventArgs e)
        {
            log.Warn("Connected to : " + Hostname);
            connected = true;
        }
        private void StreamClient_Disconnected(object sender, System.EventArgs e)
        {
            log.Warn("Disconnected to : " + Hostname);
            connected = false;
        }

    }
}
