// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using TrakHound.Api.v2.Streams;
using TrakHound.DataClient.Data;
using TrakHound.DataClient.DataGroups;

namespace TrakHound.DataClient
{
    public class DataServer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private ManualResetEvent sendStop;
        private Thread bufferThread;
        private StreamClient streamClient;

        [XmlArray("DataGroups")]
        [XmlArrayItem("DataGroup")]
        public List<DataGroup> DataGroups { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        private string _hostname;
        [XmlAttribute("hostname")]
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                _hostname = value;

                if (Buffer != null) Buffer.Hostname = _hostname;
            }
        }

        [XmlAttribute("useSSL")]
        public bool UseSSL { get; set; }

        [XmlAttribute("sendInterval")]
        public int SendInterval { get; set; }

        private Buffer _buffer;
        [XmlElement("Buffer")]
        public Buffer Buffer
        {
            get { return _buffer; }
            set
            {
                _buffer = value;
                if (_buffer != null) _buffer.Hostname = Hostname;
            }
        }

        public DataServer()
        {
            SendInterval = 5000;
        }

        public void Start()
        {
            sendStop = new ManualResetEvent(false);

            streamClient = new StreamClient(Hostname, 8472, UseSSL);
            streamClient.Timeout = 1000;
            streamClient.ReconnectionDelay = 5000;

            // Start Buffer Thread if the Buffer is configured
            if (Buffer != null)
            {
                bufferThread = new Thread(new ThreadStart(BufferWorker));
                bufferThread.Start();
            }
        }

        public void Stop()
        {
            if (sendStop != null) sendStop.Set();

            if (streamClient != null) streamClient.Close();
        }

        public void Add(IStreamData data)
        {
            Add(new List<IStreamData>() { data });
        }

        public void Add(List<IStreamData> data)
        {
            ThreadPool.QueueUserWorkItem((c) => {

                var sendList = new List<IStreamData>();

                // Add any Definitions
                sendList.AddRange(data.OfType<AgentDefinition>());
                sendList.AddRange(data.OfType<ComponentDefinition>());
                sendList.AddRange(data.OfType<DataItemDefinition>());
                sendList.AddRange(data.OfType<DeviceDefinition>());

                // Add Samples using the configured Filters
                var samples = data.OfType<Sample>().ToList();
                if (samples != null)
                {
                    var sampleSendList = new List<Sample>();

                    foreach (var dataGroup in DataGroups)
                    {
                        if (dataGroup.CaptureMode == CaptureMode.ACTIVE)
                        {
                            var filtered = samples.FindAll(o => dataGroup.CheckFilters(o));
                            foreach (var sample in filtered)
                            {
                                // Add to list if new
                                if (!sampleSendList.Exists(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id && o.Timestamp >= sample.Timestamp))
                                {
                                    sampleSendList.Add(sample);
                                }

                                // Include other DataGroups
                                foreach (var includedGroup in dataGroup.IncludedDataGroups)
                                {
                                    // Find group by name
                                    var group = DataGroups.Find(o => o.Name == includedGroup);
                                    if (group != null)
                                    {
                                        // Find most current samples for the group's filters
                                        var currentFiltered = DataClient.CurrentSamples.FindAll(o => group.CheckFilters(o));
                                        foreach (var currentSample in currentFiltered)
                                        {
                                            // Add to list if new
                                            if (!sampleSendList.Exists(o => o.DeviceId == currentSample.DeviceId && o.Id == currentSample.Id && o.Timestamp >= currentSample.Timestamp))
                                            {
                                                sampleSendList.Add(currentSample);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    sendList.AddRange(sampleSendList);
                }

                // Send filtered Samples
                if (sendList.Count > 0)
                {
                    if (!SendData(sendList))
                    {
                        if (Buffer != null)
                        {
                            Buffer.Add(sendList);
                            log.Warn(Hostname + " : " + sendList.Count + " Failed to Send. Added to Buffer");
                        }
                        else
                        {
                            log.Warn(Hostname + " : " + sendList.Count + " Failed to Send.");
                        }
                    }
                }
            });
        }

        private void BufferWorker()
        {
            do
            {
                int maxRecords = 500;

                var sendList = new List<IStreamData>();

                sendList.AddRange(Buffer.ReadCsv<AgentDefinition>(maxRecords - sendList.Count).ToList<IStreamData>());
                sendList.AddRange(Buffer.ReadCsv<ComponentDefinition>(maxRecords - sendList.Count).ToList<IStreamData>());
                sendList.AddRange(Buffer.ReadCsv<DataItemDefinition>(maxRecords - sendList.Count).ToList<IStreamData>());
                sendList.AddRange(Buffer.ReadCsv<DeviceDefinition>(maxRecords - sendList.Count).ToList<IStreamData>());
                sendList.AddRange(Buffer.ReadCsv<Sample>(maxRecords - sendList.Count).ToList<IStreamData>());

                if (sendList.Count > 0)
                {
                    var ids = sendList.Select(o => o.EntryId).ToList();

                    log.Info(Hostname + " : " + sendList.Count + " Samples Read from Buffer");

                    // Send Samples to Data Server
                    if (SendData(sendList)) Buffer.Remove(ids);
                }
            } while (!sendStop.WaitOne(2000, true));
        }
        
        public bool SendData(List<IStreamData> data)
        {
            if (data != null && data.Count > 0)
            {
                bool success = streamClient.Write(data);
                if (success) log.Info(Hostname + " : " + data.Count + " Items Sent Successfully");
                return success;
            }

            return true;
        }

    }
}
