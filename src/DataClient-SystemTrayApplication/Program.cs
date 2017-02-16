// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Windows.Forms;
using System.ServiceModel;
using System.Windows.Forms;
using WCF = TrakHound.Api.v2.WCF;
using TrakHound.DataClient.SystemTray.Messages;

namespace TrakHound.DataClient.SystemTray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("TrakHound DataClient SystemTray Started()");

            // Start MessageServer
            var MessageServer = WCF.Server.Create<MessageServer>("trakhound-dataclient-systemtray");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Instead of running a form, we run an ApplicationContext.
            Application.Run(new DataClientSystemTray());
        }
    }
}