// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using TrakHound.Api.v2.Authentication;
using TrakHound.MTConnectSniffer;
using System.Threading;

namespace TrakHound.DataClient.Configurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Configuration _configuration;


        public Device SelectedDevice
        {
            get { return (Device)GetValue(SelectedDeviceProperty); }
            set
            {
                SetSelectedDevice(value);

                SetValue(SelectedDeviceProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedDeviceProperty =
            DependencyProperty.Register("SelectedDevice", typeof(Device), typeof(MainWindow), new PropertyMetadata(null, new PropertyChangedCallback(SelectedDevice_PropertyChanged)));

        private static void SelectedDevice_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as MainWindow;
            if (o != null) o.SetSelectedDevice((Device)e.NewValue);
        }

        internal void SetSelectedDevice(Device device)
        {
            if (device != null)
            {
                SelectedDeviceAddress = device.Address;
                SelectedDevicePort = device.Port;
                SelectedDeviceName = device.DeviceName;
                SelectedDeviceInterval = device.Interval;
            }
            else
            {
                SelectedDeviceAddress = null;
                SelectedDevicePort = 5000;
                SelectedDeviceName = null;
                SelectedDeviceInterval = 500;
            }
        }


        public DataServerItem SelectedDataServer
        {
            get { return (DataServerItem)GetValue(SelectedDataServerProperty); }
            set
            {
                SetSelectedDataServer(value);

                SetValue(SelectedDataServerProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedDataServerProperty =
            DependencyProperty.Register("SelectedDataServer", typeof(DataServerItem), typeof(MainWindow), new PropertyMetadata(null, new PropertyChangedCallback(SelectedDataServer_PropertyChanged)));

        private static void SelectedDataServer_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var o = obj as MainWindow;
            if (o != null) o.SetSelectedDataServer((DataServerItem)e.NewValue);
        }

        internal void SetSelectedDataServer(DataServerItem dataServer)
        {
            if (dataServer != null)
            {
                SelectedDataServerHostname = dataServer.Hostname;
                SelectedDataServerPort = dataServer.Port;
                SelectedDataServerSendInterval = dataServer.SendInterval;
                SelectedDataServerUseSSL = dataServer.UseSSL;
            }
            else
            {
                SelectedDataServerHostname = null;
                SelectedDataServerPort = 8472;
                SelectedDataServerSendInterval = 500;
                SelectedDataServerUseSSL = false;
            }
        }


        #region "Add Device"

        public string AddDeviceAddress
        {
            get { return (string)GetValue(AddDeviceAddressProperty); }
            set { SetValue(AddDeviceAddressProperty, value); }
        }

        public static readonly DependencyProperty AddDeviceAddressProperty =
            DependencyProperty.Register("AddDeviceAddress", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int AddDevicePort
        {
            get { return (int)GetValue(AddDevicePortProperty); }
            set { SetValue(AddDevicePortProperty, value); }
        }

        public static readonly DependencyProperty AddDevicePortProperty =
            DependencyProperty.Register("AddDevicePort", typeof(int), typeof(MainWindow), new PropertyMetadata(5000));


        public string AddDeviceName
        {
            get { return (string)GetValue(AddDeviceNameProperty); }
            set { SetValue(AddDeviceNameProperty, value); }
        }

        public static readonly DependencyProperty AddDeviceNameProperty =
            DependencyProperty.Register("AddDeviceName", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

        #endregion

        #region "Selected Device"

        public string SelectedDeviceAddress
        {
            get { return (string)GetValue(SelectedDeviceAddressProperty); }
            set { SetValue(SelectedDeviceAddressProperty, value); }
        }

        public static readonly DependencyProperty SelectedDeviceAddressProperty =
            DependencyProperty.Register("SelectedDeviceAddress", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int SelectedDevicePort
        {
            get { return (int)GetValue(SelectedDevicePortProperty); }
            set { SetValue(SelectedDevicePortProperty, value); }
        }

        public static readonly DependencyProperty SelectedDevicePortProperty =
            DependencyProperty.Register("SelectedDevicePort", typeof(int), typeof(MainWindow), new PropertyMetadata(5000));


        public string SelectedDeviceName
        {
            get { return (string)GetValue(SelectedDeviceNameProperty); }
            set { SetValue(SelectedDeviceNameProperty, value); }
        }

        public static readonly DependencyProperty SelectedDeviceNameProperty =
            DependencyProperty.Register("SelectedDeviceName", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int SelectedDeviceInterval
        {
            get { return (int)GetValue(SelectedDeviceIntervalProperty); }
            set { SetValue(SelectedDeviceIntervalProperty, value); }
        }

        public static readonly DependencyProperty SelectedDeviceIntervalProperty =
            DependencyProperty.Register("SelectedDeviceInterval", typeof(int), typeof(MainWindow), new PropertyMetadata(500));

        #endregion

        #region "Add DataServer"

        public bool DataHostingSelected
        {
            get { return (bool)GetValue(DataHostingSelectedProperty); }
            set { SetValue(DataHostingSelectedProperty, value); }
        }

        public static readonly DependencyProperty DataHostingSelectedProperty =
            DependencyProperty.Register("DataHostingSelected", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));


        public bool CustomDataServerSelected
        {
            get { return (bool)GetValue(CustomDataServerSelectedProperty); }
            set { SetValue(CustomDataServerSelectedProperty, value); }
        }

        public static readonly DependencyProperty CustomDataServerSelectedProperty =
            DependencyProperty.Register("CustomDataServerSelected", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        public static readonly DependencyProperty UsernameProperty =
            DependencyProperty.Register("Username", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(MainWindow), new PropertyMetadata(null));




        public string AddDataServerHostname
        {
            get { return (string)GetValue(AddDataServerHostnameProperty); }
            set { SetValue(AddDataServerHostnameProperty, value); }
        }

        public static readonly DependencyProperty AddDataServerHostnameProperty =
            DependencyProperty.Register("AddDataServerHostname", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int AddDataServerPort
        {
            get { return (int)GetValue(AddDataServerPortProperty); }
            set { SetValue(AddDataServerPortProperty, value); }
        }

        public static readonly DependencyProperty AddDataServerPortProperty =
            DependencyProperty.Register("AddDataServerPort", typeof(int), typeof(MainWindow), new PropertyMetadata(8472));


        public int AddDataServerInterval
        {
            get { return (int)GetValue(AddDataServerIntervalProperty); }
            set { SetValue(AddDataServerIntervalProperty, value); }
        }

        public static readonly DependencyProperty AddDataServerIntervalProperty =
            DependencyProperty.Register("AddDataServerInterval", typeof(int), typeof(MainWindow), new PropertyMetadata(500));


        public bool AddDataServerUseSSL
        {
            get { return (bool)GetValue(AddDataServerUseSSLProperty); }
            set { SetValue(AddDataServerUseSSLProperty, value); }
        }

        public static readonly DependencyProperty AddDataServerUseSSLProperty =
            DependencyProperty.Register("AddDataServerUseSSL", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        #endregion

        #region "Selected DataServer"

        public string SelectedDataServerHostname
        {
            get { return (string)GetValue(SelectedDataServerHostnameProperty); }
            set { SetValue(SelectedDataServerHostnameProperty, value); }
        }

        public static readonly DependencyProperty SelectedDataServerHostnameProperty =
            DependencyProperty.Register("SelectedDataServerHostname", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public int SelectedDataServerPort
        {
            get { return (int)GetValue(SelectedDataServerPortProperty); }
            set { SetValue(SelectedDataServerPortProperty, value); }
        }

        public static readonly DependencyProperty SelectedDataServerPortProperty =
            DependencyProperty.Register("SelectedDataServerPort", typeof(int), typeof(MainWindow), new PropertyMetadata(8472));


        public bool SelectedDataServerUseSSL
        {
            get { return (bool)GetValue(SelectedDataServerUseSSLProperty); }
            set { SetValue(SelectedDataServerUseSSLProperty, value); }
        }

        public static readonly DependencyProperty SelectedDataServerUseSSLProperty =
            DependencyProperty.Register("SelectedDataServerUseSSL", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public int SelectedDataServerSendInterval
        {
            get { return (int)GetValue(SelectedDataServerSendIntervalProperty); }
            set { SetValue(SelectedDataServerSendIntervalProperty, value); }
        }

        public static readonly DependencyProperty SelectedDataServerSendIntervalProperty =
            DependencyProperty.Register("SelectedDataServerSendInterval", typeof(int), typeof(MainWindow), new PropertyMetadata(500));

        #endregion


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

        private ObservableCollection<DataServerItem> _dataServerItems;
        public ObservableCollection<DataServerItem> DataServerItems
        {
            get
            {
                if (_dataServerItems == null)
                    _dataServerItems = new ObservableCollection<DataServerItem>();
                return _dataServerItems;
            }

            set
            {
                _dataServerItems = value;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            ReadConfigurationFile();
        }

        private void ReadConfigurationFile()
        {
            Devices.Clear();
            DataServerItems.Clear();
            SelectedDevice = null;
            SelectedDataServer = null;

            // Get the default Configuration file path
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.DEFAULT_FILENAME);
            if (!File.Exists(configPath) && File.Exists(defaultPath))
            {
                File.Copy(defaultPath, configPath);
            }
            var config = Configuration.Read(configPath);
            if (config != null)
            {
                _configuration = config;

                if (config.Devices != null)
                {
                    // Devices
                    foreach (var device in config.Devices)
                    {
                        Devices.Add(device);
                    }

                    if (Devices != null && Devices.Count > 0) SelectedDevice = Devices[0];

                    // DataServers
                    foreach (var dataServer in config.DataServers)
                    {
                        DataServerItems.Add(new DataServerItem(dataServer));
                    }

                    if (DataServerItems != null && DataServerItems.Count > 0) SelectedDataServer = DataServerItems[0];
                }
            }
        }

        private void SaveDevices()
        {
            if (Devices != null)
            {
                _configuration.Devices = Devices.ToList();
                _configuration.Save();
            }
        }

        private void SaveDataServers()
        {
            if (DataServerItems != null)
            {
                _configuration.DataServers.Clear();
                foreach (var item in DataServerItems)
                {
                    _configuration.DataServers.Add(item.DataServer);
                }

                _configuration.Save();
            }
        }

        private void AddDevice_Clicked(TrakHound_UI.Button bt)
        {
            var device = new Device();
            device.DeviceId = Guid.NewGuid().ToString();
            device.Address = AddDeviceAddress;
            device.Port = AddDevicePort;
            device.DeviceName = AddDeviceName;

            Devices.Add(device);
            SelectedDevice = device;

            SaveDevices();
        }

        private void SaveDevice_Clicked(TrakHound_UI.Button bt)
        {
            if (SelectedDevice != null)
            {
                int i = Devices.ToList().FindIndex(o => o.DeviceId == SelectedDevice.DeviceId);
                if (i >= 0)
                {
                    var old = Devices[i];

                    var device = new Device();
                    device.DeviceId = old.DeviceId;
                    device.Address = SelectedDeviceAddress;
                    device.Port = SelectedDevicePort;
                    device.DeviceName = SelectedDeviceName;
                    device.Interval = SelectedDeviceInterval;
                    device.PhysicalAddress = old.PhysicalAddress;

                    Devices.RemoveAt(i);
                    Devices.Insert(i, device);
                    SelectedDevice = Devices[i];
                }

                SaveDevices();
            }         
        }

        private void DeleteDevice_Clicked(TrakHound_UI.Button bt)
        {
            if (SelectedDevice != null)
            {
                int i = Devices.ToList().FindIndex(o => o.DeviceId == SelectedDevice.DeviceId);
                if (i >= 0)
                {
                    Devices.RemoveAt(i);

                    if (Devices.Count > 0)
                    {
                        i = Math.Min(Devices.Count - 1, i);
                        SelectedDevice = Devices[i];
                    }

                    SaveDevices();
                }
            }
        }

        private void AddDataServer_Clicked(TrakHound_UI.Button bt)
        {
            // Load the Default DataServer settings to default to
            var dataServer = GetDefaultDataServer();
            if (dataServer != null)
            {
                var item = new DataServerItem(dataServer);
                item.Hostname = AddDataServerHostname;
                item.Port = AddDataServerPort;
                item.UseSSL = AddDataServerUseSSL;

                DataServerItems.Add(item);
                SelectedDataServer = item;

                SaveDataServers();
            }
            else
            {
                MessageBox.Show("Error Adding DataServer. File 'client.config.default' not found or is corrupt.", "Error Adding DataServer");
                log.Error("Get Default DataServer Error :: NOT FOUND!");
            }            
        }

        private DataServer GetDefaultDataServer()
        {
            // Get the default Configuration file path
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.DEFAULT_FILENAME);
            var config = Configuration.Read(defaultPath);
            if (config != null)
            {
                if (config.DataServers != null && config.DataServers.Count > 0)
                {
                    return config.DataServers[0];
                }
            }

            return null;
        }

        private void SaveDataServer_Clicked(TrakHound_UI.Button bt)
        {
            if (SelectedDataServer != null)
            {
                var dataServerItem = DataServerItems.ToList().Find(o => o.Id == SelectedDataServer.Id);
                if (dataServerItem != null)
                {
                    dataServerItem.Hostname = SelectedDataServerHostname;
                    dataServerItem.Port = SelectedDataServerPort;
                    dataServerItem.UseSSL = SelectedDataServerUseSSL;
                    dataServerItem.SendInterval = SelectedDataServerSendInterval;

                    SaveDataServers();
                }
            }
        }

        private void DeleteDataServer_Clicked(TrakHound_UI.Button bt)
        {
            if (SelectedDataServer != null)
            {
                int i = DataServerItems.ToList().FindIndex(o => o.Id == SelectedDataServer.Id);
                if (i >= 0)
                {
                    DataServerItems.RemoveAt(i);

                    if (DataServerItems.Count > 0)
                    {
                        i = Math.Min(DataServerItems.Count - 1, i);
                        SelectedDataServer = DataServerItems[i];
                    }

                    SaveDataServers();
                }
            }
        }

        private void Login_Clicked(TrakHound_UI.Button bt)
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                var username = Username;
                var password = Password;
                Password = null;

                var user = User.Get(username, password);
                password = null;
                if (user != null && user.ApiKey != null)
                {
                    // Load the Default DataServer settings to default to
                    var dataServer = GetDefaultDataServer();
                    if (dataServer != null)
                    {
                        var item = new DataServerItem(dataServer);
                        item.Hostname = "streaming.trakhound.com";
                        item.Port = 443;
                        item.UseSSL = true;
                        item.ApiKey = user.ApiKey.Token;

                        DataServerItems.Add(item);
                        SelectedDataServer = item;

                        SaveDataServers();
                    }
                    else
                    {
                        MessageBox.Show("Error Adding DataServer. File 'client.config.default' not found or is corrupt.", "Error Adding DataServer");
                        log.Error("Get Default DataServer Error :: NOT FOUND!");
                    }
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            new About().ShowDialog();
            IsEnabled = true;
        }

        private void FindDevices(object sender, RoutedEventArgs e)
        {
            Cursor = System.Windows.Input.Cursors.AppStarting;

            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                var sniffer = new Sniffer();
                sniffer.DeviceFound += Sniffer_DeviceFound;
                sniffer.RequestsCompleted += Sniffer_RequestsCompleted;
                sniffer.Start();
            }));
        }


        private void Sniffer_DeviceFound(MTConnectDevice device)
        {
            if (_configuration.Devices != null)
            {
                // Generate the Device ID Hash
                string deviceId = DataClient.GenerateDeviceId(device);

                // Check to make sure the Device is not already added
                if (!_configuration.Devices.Exists(o => o.DeviceId == deviceId))
                {
                    var conn = new Api.v2.Data.Connection();
                    conn.Address = device.IpAddress.ToString();
                    conn.PhysicalAddress = device.MacAddress.ToString();
                    conn.Port = device.Port;

                    // Create a new Device and start it
                    var d = new Device(deviceId, conn, device.DeviceName);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Devices.Add(d);
                    }));

                    log.Info("New Device Added : " + deviceId + " : " + device.DeviceName + " : " + device.IpAddress + " : " + device.Port);
                }
            }
        }

        private void Sniffer_RequestsCompleted(long milliseconds)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SaveDevices();

                Cursor = System.Windows.Input.Cursors.Arrow;

                MessageBox.Show("Find Devices Completed Successfully", "Find Devices Completed");
            }));
        }

        private void ReloadConfiguration_Click(object sender, RoutedEventArgs e)
        {
            ReadConfigurationFile();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
