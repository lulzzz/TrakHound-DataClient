// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace TrakHound.DataClient.Configurator
{
    /// <summary>
    /// Interaction logic for AddDevices.xaml
    /// </summary>
    public partial class AddDevices : Window
    {
        public static List<Device> Devices = new List<Device>();

        public class ListItem : Device, INotifyPropertyChanged
        {
            private bool _selected;
            public bool Selected
            {
                get { return _selected; }
                set
                {
                    SetField(ref _selected, value, "Selected");
                    SelectedChanged?.Invoke(this, null);
                }
            }

            private bool _alreadyAdded;
            public bool AlreadyAdded
            {
                get { return _alreadyAdded; }
                set { SetField(ref _alreadyAdded, value, "AlreadyAdded"); }
            }

            public event System.EventHandler SelectedChanged;


            public ListItem(Device device)
            {
                DeviceId = device.DeviceId;
                DeviceName = device.DeviceName;
                Address = device.Address;
                Port = device.Port;
                PhysicalAddress = device.PhysicalAddress;
            }

            #region "INotifyPropertyChanged"

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetField<T>(ref T field, T value, string propertyName)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            #endregion
        }

        private ObservableCollection<ListItem> _listItems;
        public ObservableCollection<ListItem> ListItems
        {
            get
            {
                if (_listItems == null) _listItems = new ObservableCollection<ListItem>();                  
                return _listItems;
            }

            set
            {
                _listItems = value;
            }
        }

        public static readonly DependencyProperty SelectedCountProperty =
            DependencyProperty.Register("SelectedCount", typeof(int), typeof(AddDevices), new PropertyMetadata(0));



        public AddDevices(Window owner)
        {
            InitializeComponent();
            DataContext = this;
            Owner = owner;

            var alreadyAddedDevices = ((MainWindow)Owner).Devices.ToList();

            int newDevices = 0;

            foreach (var device in Devices)
            {
                var listItem = new ListItem(device);
                listItem.SelectedChanged += ListItem_SelectedChanged;
                if (alreadyAddedDevices.Exists(o => o.DeviceId == device.DeviceId)) listItem.AlreadyAdded = true;
                else
                {
                    newDevices++;
                    listItem.Selected = true;
                }

                ListItems.Add(listItem);
            }

            UpdateSelectedCount();
        }

        private void ListItem_SelectedChanged(object sender, System.EventArgs e)
        {
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            SetValue(SelectedCountProperty, ListItems.ToList().FindAll(o => o.Selected == true).Count);
        }

        private void SelectAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var listItem in ListItems) if (!listItem.AlreadyAdded) listItem.Selected = true;

            UpdateSelectedCount();
        }

        private void UnselectAll_Clicked(TrakHound_UI.Button bt)
        {
            foreach (var listItem in ListItems) if (!listItem.AlreadyAdded) listItem.Selected = false;

            UpdateSelectedCount();
        }

        private void AddSelected_Clicked(TrakHound_UI.Button bt)
        {
            var mainWindow = (MainWindow)Owner;

            foreach (var listItem in ListItems.ToList().FindAll(o => o.Selected))
            {
                var device = new Device();
                device.DeviceId = listItem.DeviceId;
                device.Address = listItem.Address;
                device.Port = listItem.Port;
                device.DeviceName = listItem.DeviceName;
                device.Interval = listItem.Interval;
                device.PhysicalAddress = listItem.PhysicalAddress;

                mainWindow.Devices.Add(device);
            }

            MainWindow.SaveDevices(mainWindow.Devices.ToList());
            Close();
        }

        private void Cancel_Clicked(TrakHound_UI.Button bt)
        {
            Close();
        }
    }

    public class DataGridCellCheckBox : CheckBox
    {
        public object DataObject
        {
            get { return (object)GetValue(DataObjectProperty); }
            set { SetValue(DataObjectProperty, value); }
        }

        public static readonly DependencyProperty DataObjectProperty =
            DependencyProperty.Register("DataObject", typeof(object), typeof(DataGridCellCheckBox), new PropertyMetadata(null));
    }
}
