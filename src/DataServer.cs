// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using TrakHound.Api.v2;
using TrakHound.DataClient.DataGroups;

namespace TrakHound.DataClient
{
    public class DataServer
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private ManualResetEvent sendStop;
        private Thread bufferThread;
        private Samples.SamplesClient samplesClient;

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

            samplesClient = new Samples.SamplesClient(Hostname, 8472, UseSSL);
            samplesClient.Timeout = 1000;
            samplesClient.ReconnectionDelay = 5000;

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

            if (samplesClient != null) samplesClient.Close();
        }

        public void Add(List<DataSample> samples)
        {
            var sendList = new List<DataSample>();

            foreach (var dataGroup in DataGroups)
            {
                if (dataGroup.CaptureMode == CaptureMode.ACTIVE)
                {
                    var filtered = samples.FindAll(o => dataGroup.CheckFilters(o));
                    foreach (var sample in filtered)
                    {
                        // Add to list if new
                        if (!sendList.Exists(o => o.DeviceId == sample.DeviceId && o.Id == sample.Id && o.Timestamp >= sample.Timestamp))
                        {
                            sendList.Add(sample);
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
                                    if (!sendList.Exists(o => o.DeviceId == currentSample.DeviceId && o.Id == currentSample.Id && o.Timestamp >= currentSample.Timestamp))
                                    {
                                        sendList.Add(currentSample);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Send filtered Samples
            if (sendList.Count > 0)
            {
                if (!SendSamples(sendList))
                {
                    if (Buffer != null)
                    {
                        Buffer.Add(samples);
                        log.Warn(Hostname + " : " + sendList.Count + " Failed to Send. Added to Buffer");
                    }
                    else
                    {
                        log.Warn(Hostname + " : " + sendList.Count + " Failed to Send.");
                    }
                }
            }
        }

        public void SendDefinitions(List<ContainerDefinition> definitions)
        {
        }

        public void SendDefinitions(List<DataDefinition> definitions)
        {
        }

        private void BufferWorker()
        {
            do
            {
                // Read Samples from Buffer
                var samples = Buffer.ReadSamples(500);
                if (samples != null && samples.Count > 0)
                {
                    var ids = samples.Select(o => o.Uuid).ToList();

                    log.Info(Hostname + " : " + samples.Count + " Samples Read from Buffer");

                    // Send Samples to Data Server
                    if (SendSamples(samples)) Buffer.Remove(ids);
                }
            } while (!sendStop.WaitOne(2000, true));
        }

        public bool SendSamples(List<DataSample> samples)
        {
            if (samples != null && samples.Count > 0)
            {
                var s = new List<Samples.Sample>();
                s.AddRange(samples);

                bool success = samplesClient.Write(s);
                if (success) log.Info(Hostname + " : " + samples.Count + " Sent Successfully");
                return success;
            }

            return true;
        }

    }
}
