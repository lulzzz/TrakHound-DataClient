// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.ServiceModel;
using System.Windows.Forms;
using WCF = TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient.SystemTray
{
    public class DataClientSystemTray : ApplicationContext
    {
        public static NotifyIcon NotifyIcon = new NotifyIcon();
        private ServiceHost MessageServer;

        public DataClientSystemTray()
        {
            NotifyIcon.Text = "TrakHound DataClient";
            NotifyIcon.Icon = Properties.Resources.dataclient;

            var menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Start", Start));
            menu.MenuItems.Add(new MenuItem("Stop", Stop));
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("Open Configuration File", OpenConfigurationFile));
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("Exit", Exit));

            NotifyIcon.ContextMenu = menu;
            NotifyIcon.Visible = true;
        }

        private void Start(object sender, EventArgs e)
        {
            WCF.MessageClient.Send("trakhound-dataclient", new WCF.Message("Start"));
        }

        private void Stop(object sender, EventArgs e)
        {
            WCF.MessageClient.Send("trakhound-dataclient", new WCF.Message("Stop"));
        }

        private void OpenConfigurationFile(object sender, EventArgs e)
        {

        }

        private void Exit(object sender, EventArgs e)
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            NotifyIcon.Visible = false;

            Application.Exit();
        }
    }
}
