// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrakHound.MTConnectSniffer;

namespace TrakHound.DataClient.Configurator
{
    /// <summary>
    /// Interaction logic for FindDevices.xaml
    /// </summary>
    public partial class FindDevices : Window
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Sniffer sniffer;


        #region "Dependancy Properties"

        private ObservableCollection<Device> _devices;
        public ObservableCollection<Device> Devices
        {
            get
            {
                if (_devices == null)
                    _devices = new ObservableCollection<Device>();
                return _devices;
            }

            set
            {
                _devices = value;
            }
        }

        private ObservableCollection<string> _logLines;
        public ObservableCollection<string> LogLines
        {
            get
            {
                if (_logLines == null)
                    _logLines = new ObservableCollection<string>();
                return _logLines;
            }

            set
            {
                _logLines = value;
            }
        }


        public string LogText
        {
            get { return (string)GetValue(LogTextProperty); }
            set { SetValue(LogTextProperty, value); }
        }

        public static readonly DependencyProperty LogTextProperty =
            DependencyProperty.Register("LogText", typeof(string), typeof(FindDevices), new PropertyMetadata(""));


        public int LogTextSelectedIndex
        {
            get { return (int)GetValue(LogTextSelectedIndexProperty); }
            set { SetValue(LogTextSelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty LogTextSelectedIndexProperty =
            DependencyProperty.Register("LogTextSelectedIndex", typeof(int), typeof(FindDevices), new PropertyMetadata(0));


        public string AddressFrom
        {
            get { return (string)GetValue(AddressFromProperty); }
            set { SetValue(AddressFromProperty, value); }
        }

        public static readonly DependencyProperty AddressFromProperty =
            DependencyProperty.Register("AddressFrom", typeof(string), typeof(FindDevices), new PropertyMetadata(null));


        public string AddressTo
        {
            get { return (string)GetValue(AddressToProperty); }
            set { SetValue(AddressToProperty, value); }
        }

        public static readonly DependencyProperty AddressToProperty =
            DependencyProperty.Register("AddressTo", typeof(string), typeof(FindDevices), new PropertyMetadata(null));


        public int PortFrom
        {
            get { return (int)GetValue(PortFromProperty); }
            set { SetValue(PortFromProperty, value); }
        }

        public static readonly DependencyProperty PortFromProperty =
            DependencyProperty.Register("PortFrom", typeof(int), typeof(FindDevices), new PropertyMetadata(5000));


        public int PortTo
        {
            get { return (int)GetValue(PortToProperty); }
            set { SetValue(PortToProperty, value); }
        }

        public static readonly DependencyProperty PortToProperty =
            DependencyProperty.Register("PortTo", typeof(int), typeof(FindDevices), new PropertyMetadata(5019));


        public bool Started
        {
            get { return (bool)GetValue(StartedProperty); }
            set { SetValue(StartedProperty, value); }
        }

        public static readonly DependencyProperty StartedProperty =
            DependencyProperty.Register("Started", typeof(bool), typeof(FindDevices), new PropertyMetadata(false));


        public bool Running
        {
            get { return (bool)GetValue(RunningProperty); }
            set { SetValue(RunningProperty, value); }
        }

        public static readonly DependencyProperty RunningProperty =
            DependencyProperty.Register("Running", typeof(bool), typeof(FindDevices), new PropertyMetadata(false));

        #endregion


        public FindDevices()
        {
            InitializeComponent();
            DataContext = this;
            Closed += FindDevices_Closed;

            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Save();

            // Load Address Range
            AddressFrom = Properties.Settings.Default.AddressFrom;
            AddressTo = Properties.Settings.Default.AddressTo;
            if (string.IsNullOrEmpty(AddressFrom) || string.IsNullOrEmpty(AddressTo))
            {
                var hostAddress = GetHostAddress();
                if (hostAddress != null)
                {
                    IPNetwork ip;
                    if (IPNetwork.TryParse(hostAddress.ToString(), out ip))
                    {
                        var addresses = IPNetwork.ListIPAddress(ip);
                        if (addresses != null)
                        {
                            AddressFrom = addresses.First().ToString();
                            AddressTo = addresses.Last().ToString();
                        }
                    }
                }
            }

            // Load Port Range
            PortFrom = Properties.Settings.Default.PortFrom;
            PortTo = Properties.Settings.Default.PortTo;
        }

        private void FindDevices_Closed(object sender, EventArgs e)
        {
            // Save Address Range
            Properties.Settings.Default.AddressFrom = AddressFrom;
            Properties.Settings.Default.AddressTo = AddressTo;

            // Save Port Range
            Properties.Settings.Default.PortFrom = PortFrom;
            Properties.Settings.Default.PortTo = PortTo;

            Properties.Settings.Default.Save();

            Cancel();
        }


        private void Start()
        {
            Devices.Clear();

            Started = true;
            LogText = "";

            if (sniffer != null) sniffer.Stop();

            var addresses = GetAddressRange();
            var ports = GetPortRange();

            if (addresses.Length > 0 && ports.Length > 0)
            {
                Running = true;

                ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
                {
                    sniffer = new Sniffer();
                    sniffer.AddressRange = addresses;
                    sniffer.PortRange = ports;
                    sniffer.DeviceFound += Sniffer_DeviceFound;
                    sniffer.RequestsCompleted += Sniffer_RequestsCompleted;
                    sniffer.PingSent += Sniffer_PingSent;
                    sniffer.PingReceived += Sniffer_PingReceived;
                    sniffer.PortOpened += Sniffer_PortOpened;
                    sniffer.PortClosed += Sniffer_PortClosed;
                    sniffer.ProbeSent += Sniffer_ProbeSent;
                    sniffer.ProbeSuccessful += Sniffer_ProbeSuccessful;
                    sniffer.ProbeError += Sniffer_ProbeError;
                    sniffer.Start();
                }));
            }
            else
            {
                if (addresses.Length == 0) Log("Invalid Address Range : Please specify a From Address and To Address");
                if (ports.Length == 0) Log("Invalid Ports Range : Please specify a From Port and To Port");
            }
        }

        private void Cancel()
        {
            Running = false;

            if (sniffer != null) sniffer.Stop();
        }

        private void Next()
        {
            AddDevices.Devices.Clear();
            foreach (var device in Devices) AddDevices.Devices.Add(device);

            DialogResult = true;
            Close();
        }


        private void Start_Clicked(TrakHound_UI.Button bt) { Start(); }

        private void Cancel_Clicked(TrakHound_UI.Button bt) { Cancel(); }

        private void Next_Clicked(TrakHound_UI.Button bt) { Next(); }


        private void Sniffer_DeviceFound(MTConnectDevice device)
        {
            // Generate the Device ID Hash
            string deviceId = DataClient.GenerateDeviceId(device);

            // Create a new Connection object
            var conn = new Api.v2.Data.Connection();
            conn.Address = device.IpAddress.ToString();
            conn.PhysicalAddress = device.MacAddress.ToString();
            conn.Port = device.Port;

            // Create a new Device and start it
            var d = new Device(deviceId, conn, device.DeviceName);

            // Add Device to List
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Devices.Add(d);
            }));

            // Write to Logs
            string msg = "Device Found : " + deviceId + " : " + device.DeviceName + " : " + device.IpAddress + " : " + device.Port;
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_RequestsCompleted(long milliseconds)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Running = false;

                if (Devices.Count == 0) MessageBox.Show("No Devices Were Found");
            }));
        }

        private void Sniffer_PingSent(IPAddress address)
        {
            // Write to Logs
            string msg = "Ping Sent : " + address.ToString();
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_PingReceived(IPAddress address, System.Net.NetworkInformation.PingReply reply)
        {
            // Write to Logs
            string msg = "Ping Received : " + address.ToString() + " : " + reply.Status + " in " + reply.RoundtripTime + "ms";
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_PortOpened(IPAddress address, int port)
        {
            // Write to Logs
            string msg = "Port Open : " + address.ToString() + ":" + port;
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_PortClosed(IPAddress address, int port)
        {
            // Write to Logs
            string msg = "Port Closed : " + address.ToString() + ":" + port;
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_ProbeSent(IPAddress address, int port)
        {
            // Write to Logs
            string msg = "Probe Sent : " + address.ToString() + ":" + port;
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_ProbeSuccessful(IPAddress address, int port)
        {
            // Write to Logs
            string msg = "Probe Successful : " + address.ToString() + ":" + port;
            Log(msg);
            log.Info(msg);
        }

        private void Sniffer_ProbeError(IPAddress address, int port)
        {
            // Write to Logs
            string msg = "Probe Error : " + address.ToString() + ":" + port;
            Log(msg);
            log.Info(msg);
        }


        private IPAddress GetHostAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces != null)
            {
                var addresses = new List<IPAddress>();

                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    {
                        foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private IPAddress[] GetAddressRange()
        {
            var l = new List<IPAddress>();

            IPAddress min;
            IPAddress max;

            IPAddress.TryParse(AddressFrom, out min);
            IPAddress.TryParse(AddressTo, out max);

            if (min != null && max != null)
            {
                var minBytes = min.GetAddressBytes();
                var maxBytes = max.GetAddressBytes();

                var b = minBytes[3];
                var e = maxBytes[3];

                for (int i = b; i <= e; i++)
                {
                    byte x = (byte)i; 
                    l.Add(new IPAddress(new byte[] { minBytes[0], minBytes[1], minBytes[2], x }));
                }

            }

            return l.ToArray();
        }

        private static IPAddress[] GetIpAddressFromString(string[] strings)
        {
            var l = new List<IPAddress>();

            foreach (var s in strings)
            {
                IPAddress ip;
                if (IPAddress.TryParse(s, out ip)) l.Add(ip);
            }

            return l.ToArray();
        }

        private int[] GetPortRange()
        {
            var l = new List<int>();

            for (int i = PortFrom; i <= PortTo; i++) l.Add(i);

            return l.ToArray();
        }


        private void Log(string line)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Add line to list
                LogLines.Add(line);

                // Scroll To Bottom
                if (VisualTreeHelper.GetChildrenCount(LogListBox) > 0)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(LogListBox, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }));
        }

        private void CopyLogToClipboard_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string text = "";

            foreach (var line in LogLines) text += line + Environment.NewLine;

            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }
    }
}
