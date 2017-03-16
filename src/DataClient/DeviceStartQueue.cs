// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TrakHound.DataClient
{
    class DeviceStartQueue
    {
        private object _lock = new object();

        private List<Device> queue = new List<Device>();

        private ManualResetEvent stop;
        private Thread thread;

        public delegate void DeviceStartedHandler(Device device);
        public event DeviceStartedHandler DeviceStarted;

        public int Delay { get; set; }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    if (queue != null) return queue.Count;
                }

                return -1;
            }
        }


        public DeviceStartQueue()
        {
            Delay = 2000;
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (stop != null) stop.Set();
            }
        }

        public void Add(Device device)
        {
            if (device != null)
            {
                lock (_lock) queue.Add(device);
            }
        }

        private void Worker()
        {
            do
            {
                List<Device> devices = null;

                lock (_lock)
                {
                    devices = queue.ToList();
                }

                if (devices != null && devices.Count > 0)
                {
                    var device = devices[0];
                    device.Start();

                    DeviceStarted?.Invoke(device);

                    // Remove from queue
                    lock (_lock) queue.RemoveAll(o => o.DeviceId == device.DeviceId);
                }
            } while (!stop.WaitOne(Delay, true));
        }
    }
}
