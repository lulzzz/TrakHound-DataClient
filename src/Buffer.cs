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
using TrakHound.Api.v2.Streams;
using TrakHound.DataClient.Data;

namespace TrakHound.DataClient
{
    public class Buffer
    {
        public const string FILENAME_AGENT_DEFINITIONS = "agent_definitions";
        public const string FILENAME_COMPONENT_DEFINITIONS = "component_definitions";
        public const string FILENAME_DATA_ITEM_DEFINITIONS = "data_item_definitions";
        public const string FILENAME_DEVICE_DEFINITIONS = "device_definitions";
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
        public List<IStreamData> Data { get; set; }

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

            Data = new List<IStreamData>();

            writeStop = new ManualResetEvent(false);
            writeThread = new Thread(new ThreadStart(WriteWorker));
            writeThread.Start();
        }

        public void Close()
        {
            if (writeStop != null) writeStop.Set();
        }

        public void Add(IStreamData data)
        {
            lock(_lock)
            {
                Data.Add(data);
            }        
        }

        public void Add(List<IStreamData> data)
        {
            lock (_lock) 
            {
                Data.AddRange(data);
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
            // List of data that was succesfully written to file
            var l = new List<string>();

            List<IStreamData> data;
            lock (_lock) data = Data.ToList();
            if (data != null && data.Count > 0)
            {
                // Write Agent Definitions
                l.AddRange(WriteCsv(data.OfType<AgentDefinition>().ToList<IStreamData>(), StreamDataType.AGENT_DEFINITION));

                // Write Component Defintions
                l.AddRange(WriteCsv(data.OfType<ComponentDefinition>().ToList<IStreamData>(), StreamDataType.COMPONENT_DEFINITION));

                // Write DataItem Defintions
                l.AddRange(WriteCsv(data.OfType<DataItemDefinition>().ToList<IStreamData>(), StreamDataType.DATA_ITEM_DEFINITION));

                // Write Device Defintions
                l.AddRange(WriteCsv(data.OfType<DeviceDefinition>().ToList<IStreamData>(), StreamDataType.DEVICE_DEFINITION));

                // Write Samples
                l.AddRange(WriteCsv(data.OfType<Sample>().ToList<IStreamData>(), StreamDataType.SAMPLE));
                

                // Remove from List
                lock (_lock)
                {
                    Data.RemoveAll(o => l.Contains(o.EntryId));
                }
            }
        }

        private List<string> WriteCsv(List<IStreamData> data, StreamDataType type)
        {
            var stored = new List<string>();

            try
            {
                do
                {
                    string path = GetPath(type);

                    // Start Append FileStream
                    using (var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        foreach (var item in data)
                        {
                            // Write CSV lines
                            string s = Csv.ToCsv(item) + Environment.NewLine;
                            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                            fileStream.Write(bytes, 0, bytes.Length);
                            stored.Add(item.EntryId);

                            // Check file size limit
                            if (fileStream.Length >= (MaxFileSize - BUFFER_FILE_PADDING)) break;
                        }
                    }
                } while (stored.Count < data.Count);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return stored;
        }

        private string GetPath(StreamDataType type)
        {
            string file = null;

            switch (type)
            {
                case StreamDataType.AGENT_DEFINITION: file = FILENAME_AGENT_DEFINITIONS; break;
                case StreamDataType.COMPONENT_DEFINITION: file = FILENAME_COMPONENT_DEFINITIONS; break;
                case StreamDataType.DATA_ITEM_DEFINITION: file = FILENAME_DATA_ITEM_DEFINITIONS; break;
                case StreamDataType.DEVICE_DEFINITION: file = FILENAME_DEVICE_DEFINITIONS; break;
                case StreamDataType.SAMPLE: file = FILENAME_SAMPLES; break;
            }

            if (file != null)
            {
                // Get the Parent Directory
                string dir = GetDirectory();
                System.IO.Directory.CreateDirectory(dir);

                string filename = Path.ChangeExtension(file, "csv");
                string path = Path.Combine(dir, filename);

                // Increment Filename until Size is ok
                int i = 1;
                while (!IsFileOk(path))
                {
                    filename = Path.ChangeExtension(file + "_" + i, "csv");
                    path = Path.Combine(dir, filename);
                    i++;
                }

                return path;
            }

            return null;
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
        
        public List<T> ReadCsv<T>(int maxRecords)
        {
            int i = 0;

            // Get list of Sample Buffer Files
            var dir = GetDirectory();
            if (System.IO.Directory.Exists(dir))
            {
                string f = null;

                if (typeof(T) == typeof(AgentDefinition)) f = FILENAME_AGENT_DEFINITIONS;
                else if (typeof(T) == typeof(ComponentDefinition)) f = FILENAME_COMPONENT_DEFINITIONS;
                else if (typeof(T) == typeof(DataItemDefinition)) f = FILENAME_DATA_ITEM_DEFINITIONS;
                else if (typeof(T) == typeof(DeviceDefinition)) f = FILENAME_DEVICE_DEFINITIONS;
                else if (typeof(T) == typeof(Sample)) f = FILENAME_SAMPLES;

                var buffers = System.IO.Directory.GetFiles(GetDirectory(), f + "*");
                if (buffers != null)
                {
                    var data = new List<T>();

                    // Read each Buffer file
                    foreach (var buffer in buffers)
                    {
                        var s = ReadCsv<T>(buffer, maxRecords - i);
                        if (s != null)
                        {
                            i += s.Count;
                            data.AddRange(s);

                            if (i >= s.Count) break;
                        }
                    }

                    return data;
                }
            }

            return null;
        }

        private List<T> ReadCsv<T>(string path, int maxRecords)
        {
            if (File.Exists(path))
            {
                int readRecords = 0;

                try
                {
                    var d = new List<T>();

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream && readRecords < maxRecords)
                        {
                            // Read record
                            var line = reader.ReadLine();
                            readRecords++;

                            // Get object from Csv record
                            var data = Csv.FromCsv<T>(line);
                            if (data != null) d.Add(data);
                        }
                    }

                    return d;
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
            var buffers = System.IO.Directory.GetFiles(GetDirectory());
            if (buffers != null)
            {
                // Read each Buffer file
                foreach (var buffer in buffers)
                {
                    if (!Remove(buffer, ids)) return false;
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
                    var d = new List<IStreamData>();

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            var data = Csv.FromCsv<IStreamData>(line);
                            if (data != null && !ids.Exists(o => o == data.EntryId))
                            {
                                d.Add(data);
                            }
                        }

                        // Write un removed records back to file
                        using (var writer = new StreamWriter(tempFile))
                        {
                            foreach (var data in d)
                            {
                                string csv = Csv.ToCsv(data);
                                if (!string.IsNullOrEmpty(csv))
                                {
                                    writer.WriteLine(csv);
                                }
                            }
                        }
                    }

                    File.Delete(path);
                    if (d.Count > 0) File.Move(tempFile, path);

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
