// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace TrakHound.DataClient
{
    public class Buffer
    {
        public const string FILENAME_CONTAINER_DEFINITION = "container_definitions";
        public const string FILENAME_DATA_DEFINITION = "data_definitions";
        public const string FILENAME_SAMPLES = "samples";

        private const int BUFFER_FILE_PADDING = 100;

        private static Logger log = LogManager.GetCurrentClassLogger();

        [XmlText]
        public string Directory { get; set; }

        [XmlAttribute("maxFileSize")]
        public long MaxFileSize { get; set; }

        [XmlIgnore]
        public string Hostname { get; set; }

        [XmlIgnore]
        public List<DeviceDefinition> DeviceDefinitions { get; set; }

        [XmlIgnore]
        public List<ComponentDefinition> ComponentDefinitions { get; set; }

        [XmlIgnore]
        public List<DataItemDefinition> DataItemDefinitions { get; set; }

        [XmlIgnore]
        public List<DataSample> DataSamples { get; set; }

        private Thread writeThread;
        private ManualResetEvent writeStop;

        private object _lock = new object();

        public Buffer()
        {
            Init();
        }

        private void Init()
        {
            MaxFileSize = 1048576 * 100; // 100 MB

            DeviceDefinitions = new List<DeviceDefinition>();
            ComponentDefinitions = new List<ComponentDefinition>();
            DataItemDefinitions = new List<DataItemDefinition>();
            DataSamples = new List<DataSample>();

            writeStop = new ManualResetEvent(false);
            writeThread = new Thread(new ThreadStart(WriteWorker));
            writeThread.Start();
        }

        public void Close()
        {
            if (writeStop != null) writeStop.Set();
        }

        public void Add(DataSample sample)
        {
            lock(_lock)
            {
                DataSamples.Add(sample);
            }        
        }

        public void Add(List<DataSample> samples)
        {
            lock (_lock)
            {
                DataSamples.AddRange(samples);
            }
        }

        private void WriteWorker()
        {
            while (!writeStop.WaitOne(2000, true))
            {
                WriteCsv();
            }
        }

        private string GetDirectory()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(Directory)) dir = Directory;

            if (!string.IsNullOrEmpty(Hostname)) dir = Path.Combine(dir, ConvertToFileName(Hostname));

            return dir;
        }

        public void WriteCsv()
        {
            // List of Samples that were written to file
            var storedSamples = new List<string>();

            List<DataSample> samples;
            lock (_lock) samples = DataSamples.ToList();
            if (samples != null && samples.Count > 0)
            {
                try
                {
                    do
                    {
                        string path = GetSamplesPath();

                        // Start Append FileStream
                        using (var fileStream = new FileStream(path, FileMode.Append))
                        {
                            foreach (var sample in samples)
                            {
                                string s = sample.ToCsv() + Environment.NewLine;
                                var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                                fileStream.Write(bytes, 0, bytes.Length);
                                storedSamples.Add(sample.Uuid);

                                // Check file size limit
                                if (fileStream.Length >= (MaxFileSize - BUFFER_FILE_PADDING)) break;
                            }
                        }
                    } while (storedSamples.Count < samples.Count);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
               
                // Remove from List
                lock (_lock)
                {
                    int count = DataSamples.Count;
                    DataSamples.RemoveAll(o => storedSamples.Contains(o.Uuid));
                }
            }
        }

        private string GetSamplesPath()
        {
            // Get the Parent Directory
            string dir = GetDirectory();
            System.IO.Directory.CreateDirectory(dir);

            string filename = Path.ChangeExtension(FILENAME_SAMPLES, "csv");
            string path = Path.Combine(dir, filename);

            // Increment Filename until Size is ok
            int i = 1;
            while (!IsFileOk(path))
            {
                filename = Path.ChangeExtension(FILENAME_SAMPLES + "_" + i, "csv");
                path = Path.Combine(dir, filename);
                i++;
            }

            return path;
        }

        private bool IsFileOk(string path)
        {
            if (!File.Exists(path)) return true;
            else
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo != null)
                    {
                        return fileInfo.Length < (MaxFileSize - BUFFER_FILE_PADDING);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return false;
        }

        private static string ConvertToFileName(string url)
        {
            List<string> urlParts = new List<string>();
            string rt = "";
            var r = new Regex(@"[a-z]+", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(url))
            {
                urlParts.Add(m.Value);
            }
            int c = urlParts.Count;
            for (int i = 0; i < c; i++)
            {
                rt = rt + urlParts[i];
                if (i < c - 1) rt = rt + "_";
            }
            return rt;
        }


        public List<DataSample> ReadSamples(int maxRecords)
        {
            int i = 0;

            // Get list of Sample Buffer Files
            var dir = GetDirectory();
            if (System.IO.Directory.Exists(dir))
            {
                var sampleBuffers = System.IO.Directory.GetFiles(GetDirectory(), "samples*");
                if (sampleBuffers != null)
                {
                    var samples = new List<DataSample>();

                    // Read each Buffer file
                    foreach (var sampleBuffer in sampleBuffers)
                    {
                        var s = ReadSamples(sampleBuffer, maxRecords - i);
                        if (s != null)
                        {
                            i += s.Count;
                            samples.AddRange(s);

                            if (i >= s.Count) break;
                        }
                    }

                    return samples;
                }
            }

            return null;
        }

        private List<DataSample> ReadSamples(string path, int maxRecords)
        {
            if (File.Exists(path))
            {
                int readRecords = 0;

                try
                {
                    var samples = new List<DataSample>();

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream && readRecords < maxRecords)
                        {
                            var line = reader.ReadLine();
                            readRecords++;

                            var sample = DataSample.FromCsv(line);
                            if (sample != null) samples.Add(sample);
                        }
                    }

                    return samples;
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return null;
        }


        public bool Remove(List<string> ids)
        {
            // Get list of Sample Buffer Files
            var sampleBuffers = System.IO.Directory.GetFiles(GetDirectory(), "samples*");
            if (sampleBuffers != null)
            {
                // Read each Buffer file
                foreach (var sampleBuffer in sampleBuffers)
                {
                    if (!Remove(sampleBuffer, ids)) return false;
                }
            }

            return true;
        }

        public bool Remove(string path, List<string> ids)
        {
            if (File.Exists(path))
            {
                var tempFile = Path.GetTempFileName();

                try
                {
                    var samples = new List<DataSample>();

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            var sample = DataSample.FromCsv(line);
                            if (sample != null && !ids.Exists(o => o == sample.Uuid))
                            {
                                samples.Add(sample);
                            }
                        }

                        // Write un removed records back to file
                        using (var writer = new StreamWriter(tempFile))
                        {
                            foreach (var sample in samples)
                            {
                                writer.WriteLine(sample.ToCsv());
                            }
                        }
                    }

                    File.Delete(path);
                    if (samples.Count > 0) File.Move(tempFile, path);

                    return true;
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }

            return false;
        }
        
    }
}
