// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WCF = TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient.SystemTray
{
    public class DataClientSystemTray : ApplicationContext
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static MenuItem StatusMenuItem = new MenuItem() { Enabled = false };

        public static NotifyIcon NotifyIcon = new NotifyIcon();
       

        public DataClientSystemTray()
        {
            NotifyIcon.Text = "TrakHound DataClient";
            NotifyIcon.Icon = Properties.Resources.dataclient;

            var menu = new ContextMenu();
            
            menu.MenuItems.Add(StatusMenuItem);
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("Start", Start));
            menu.MenuItems.Add(new MenuItem("Stop", Stop));
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("Open Configuration File", OpenConfigurationFile));
            menu.MenuItems.Add(new MenuItem("Open Log File", OpenLogFile));
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("Exit", Exit));

            NotifyIcon.ContextMenu = menu;
            NotifyIcon.Visible = true;
        }

        private void Start(object sender, EventArgs e)
        {
            WCF.MessageClient.Send("trakhound-dataclient", new WCF.Message("command", "Start"));
        }

        private void Stop(object sender, EventArgs e)
        {
            WCF.MessageClient.Send("trakhound-dataclient", new WCF.Message("command", "Stop"));
        }

        private void OpenConfigurationFile(object sender, EventArgs e)
        {
            try
            {
                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string configPath = Path.Combine(appDir, "client.config");

                if (File.Exists(configPath)) Process.Start(configPath);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void OpenLogFile(object sender, EventArgs e)
        {
            try
            {
                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string configPath = Path.Combine(appDir, "error.log");

                if (File.Exists(configPath)) Process.Start(configPath);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public static void SetHeader(string text)
        {
            StatusMenuItem.Text = text;
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
