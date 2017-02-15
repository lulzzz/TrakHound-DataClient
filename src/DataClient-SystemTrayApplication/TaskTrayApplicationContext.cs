// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Windows.Forms;

namespace TrakHound.DataClient.SystemTray
{
    public class TaskTrayApplicationContext : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        public TaskTrayApplicationContext()
        {
            var exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));

            notifyIcon.Icon = Properties.Resources.AppIcon;
            notifyIcon.DoubleClick += new EventHandler(ShowMessage);
            var menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Exit", ShowMessage));
            notifyIcon.ContextMenu = menu;
            notifyIcon.Visible = true;
        }

        void ShowMessage(object sender, EventArgs e)
        {
            // Only show the message if the settings say we can.
            if (Properties.Settings.Default.ShowMessage)
                MessageBox.Show("Hello World");
        }

        void Exit(object sender, EventArgs e)
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            notifyIcon.Visible = false;

            Application.Exit();
        }
    }
}
