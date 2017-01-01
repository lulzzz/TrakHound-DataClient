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
    /// <summary>
    /// Handles the buffering of data when it failed to send successfully from a DataServer
    /// </summary>
    public class Buffer
    {
        public const string FILENAME_AGENT_DEFINITIONS = "agent_definitions";
        public const string FILENAME_COMPONENT_DEFINITIONS = "component_definitions";
        public const string FILENAME_DATA_ITEM_DEFINITIONS = "data_item_definitions";
        public const string FILENAME_DEVICE_DEFINITIONS = "device_definitions";
        public const string FILENAME_SAMPLES = "samples";

        private static Logger log = LogManager.GetCurrentClassLogger();

        private string _directory { get; set; }
        /// <summary>
        /// Gets the directory that the buffer writes to. Read Only.
        /// </summary>
        [XmlText]
        public string Directory
        {
            get { return _directory; }
            set
            {
                if (_directory != null) throw new InvalidOperationException("Cannot set value. Directory is ReadOnly!");
                _directory = value;
            }
        }

        /// <summary>
        /// Gets or Sets the maximum file size that each buffer file should be.
        /// </summary>
        [XmlAttribute("maxFileSize")]
        public long MaxFileSize { get; set; }

        internal string _hostname;
        /// <summary>
        /// Gets the Hostname of the DataServer that the buffer is for
        /// </summary>
        [XmlIgnore]
        public string Hostname { get { return _hostname; } }

        private List<StreamData> data = new List<StreamData>();
        private Thread thread;
        private ManualResetEvent stop;
        private object _lock = new object();

        public Buffer()
        {
            Init();
        }

        public Buffer(string directory)
        {
            Init();
            _directory = directory;
        }

        private void Init()
        {
            MaxFileSize = 1048576 * 100; // 100 MB
        }

        /// <summary>
        /// Start the Buffer Read/Write thread
        /// </summary>
        public void Start(string hostname)
        {
            _hostname = hostname;

            stop = new ManualResetEvent(false);

            thread = new Thread(new ThreadStart(WriteWorker));
            thread.Start();
        }

        /// <summary>
        /// Stop the Buffer
        /// </summary>
        public void Stop()
        {
            if (stop != null) stop.Set();
        }


        /// <summary>
        /// Add a single StreamData item
        /// </summary>
        public void Add(StreamData streamData)
        {
            lock(_lock)
            {
                data.Add(streamData);
            }        
        }

        /// <summary>
        /// Add a list of StreamData items
        /// </summary>
        public void Add(List<StreamData> streamData)
        {
            lock (_lock) 
            {
                data.AddRange(streamData);
            }
        }

        /// <summary>
        /// Read a number of items from the Buffer
        /// </summary>
        /// <typeparam name="T">Type of item to read</typeparam>
        /// <param name="maxRecords">Maximum number of items to read</param>
        public List<T> Read<T>(int maxRecords)
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
                        var s = ReadFromFile<T>(buffer, maxRecords - i);
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

        /// <summary>
        /// Remove a list of Ids from the buffer files
        /// </summary>
        public bool Remove(List<string> ids)
        {
            // Get list of Sample Buffer Files
            var buffers = System.IO.Directory.GetFiles(GetDirectory());
            if (buffers != null)
            {
                // Read each Buffer file
                foreach (var buffer in buffers)
                {
                    if (!RemoveFromFile(buffer, ids)) return false;
                }
            }

            return true;
        }


        private void WriteWorker()
        {
            while (!stop.WaitOne(2000, true))
            {
                WriteToFile();
            }
        }

        private void WriteToFile()
        {
            // List of data that was succesfully written to file
            var ids = new List<string>();

            // Create a temporary list (to not lock up original list)
            List<StreamData> temp;
            lock (_lock) temp = data.ToList();
            if (temp != null && temp.Count > 0)
            {
                // Write Agent Definitions
                ids.AddRange(WriteToFile(temp.OfType<AgentDefinition>().ToList<StreamData>(), StreamDataType.AGENT_DEFINITION));

                // Write Component Defintions
                ids.AddRange(WriteToFile(temp.OfType<ComponentDefinition>().ToList<StreamData>(), StreamDataType.COMPONENT_DEFINITION));

                // Write DataItem Defintions
                ids.AddRange(WriteToFile(temp.OfType<DataItemDefinition>().ToList<StreamData>(), StreamDataType.DATA_ITEM_DEFINITION));

                // Write Device Defintions
                ids.AddRange(WriteToFile(temp.OfType<DeviceDefinition>().ToList<StreamData>(), StreamDataType.DEVICE_DEFINITION));

                // Write Samples
                ids.AddRange(WriteToFile(temp.OfType<Sample>().ToList<StreamData>(), StreamDataType.SAMPLE));


                // Remove from List
                lock (_lock)
                {
                    data.RemoveAll(o => ids.Contains(o.EntryId));
                }
            }
        }

        private List<string> WriteToFile(List<StreamData> streamData, StreamDataType type)
        {
            // Create a list with the list of EntryIds of each successfully written item
            var written = new List<string>();

            try
            {
                do
                {
                    // Get the filepath based on the Type of StreamData being written
                    string path = GetPath(type);

                    // Start Append FileStream
                    using (var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        foreach (var item in streamData)
                        {
                            // Write CSV lines
                            string s = Csv.ToCsv(item) + Environment.NewLine;
                            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                            fileStream.Write(bytes, 0, bytes.Length);
                            written.Add(item.EntryId);

                            // Check file size limit
                            if (fileStream.Length >= MaxFileSize) break;
                        }
                    }
                } while (written.Count < streamData.Count);
            }
            catch (Exception ex)
            {
                log.Trace(ex);
            }

            return written;
        }

        private List<T> ReadFromFile<T>(string path, int maxRecords)
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
                    log.Trace(ex);
                }
            }

            return null;
        }

        private bool RemoveFromFile(string path, List<string> ids)
        {
            if (File.Exists(path))
            {
                var tempFile = Path.GetTempFileName();

                try
                {
                    Type t;

                    string filename = Path.GetFileNameWithoutExtension(path);

                    var d = new List<StreamData>();

                    using (var f = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var reader = new StreamReader(f))
                    {
                        // Read records from file
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            StreamData item = null;

                            if (filename.StartsWith(FILENAME_AGENT_DEFINITIONS)) item = Csv.FromCsv<AgentDefinition>(line);
                            if (filename.StartsWith(FILENAME_DEVICE_DEFINITIONS)) item = Csv.FromCsv<DeviceDefinition>(line);
                            if (filename.StartsWith(FILENAME_COMPONENT_DEFINITIONS)) item = Csv.FromCsv<ComponentDefinition>(line);
                            if (filename.StartsWith(FILENAME_DATA_ITEM_DEFINITIONS)) item = Csv.FromCsv<DataItemDefinition>(line);
                            if (filename.StartsWith(FILENAME_SAMPLES)) item = Csv.FromCsv<Sample>(line);

                            if (item != null && !ids.Exists(o => o == item.EntryId))
                            {
                                d.Add(item);
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
                    log.Trace(ex);
                }
            }

            return false;
        }

        private string GetDirectory()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(Directory)) dir = Directory;

            if (!string.IsNullOrEmpty(Hostname)) dir = Path.Combine(dir, ConvertToFileName(Hostname));

            return dir;
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
                        return fileInfo.Length < MaxFileSize;
                    }
                }
                catch (Exception ex)
                {
                    log.Trace(ex);
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

    }
}
