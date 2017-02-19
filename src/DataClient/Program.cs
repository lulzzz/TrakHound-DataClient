// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;
using WCF = TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient
{
    static class Program
    {
        private const int MENU_UPDATE_INTERVAL = 2000;

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static DataClient client;
        private static ServiceBase service;
        internal static bool RunAsService;
        private static Timer menuUpdateTimer;
        private static bool started = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            Init(args);
        }

        private static void Init(string[] args)
        {
            if (args.Length > 0)
            {
                string mode = args[0];

                switch (mode)
                {
                    // Debug (Run as console application)
                    case "debug":

                        Start();
                        Console.ReadLine();
                        Stop();
                        Console.ReadLine();
                        break;

                    // Install the Service
                    case "install":

                        InstallService();
                        break;

                    // Uninstall the Service
                    case "uninstall":

                        UninstallService();
                        break;
                }
            }
            else
            {
                RunAsService = true;

                StartService();
            }
        }

        public static void StartService()
        {
            if (service == null) service = new DataClientService();
            ServiceBase.Run(service);
        }

        public static void StopService()
        {
            if (service != null) service.Stop();
        }

        public static void Start()
        {
            // Get the default Configuration file path
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);
            var config = Configuration.Read(configPath);
            if (config != null)
            {
                log.Info("Configuration file read from '" + configPath + "'");
                log.Info("---------------------------");

                if (config.SendMessages)
                {
                    // Start Menu Update Timer
                    menuUpdateTimer = new Timer();
                    menuUpdateTimer.Elapsed += UpdateMenuStatus;
                    menuUpdateTimer.Interval = MENU_UPDATE_INTERVAL;
                    menuUpdateTimer.Start();
                }

                // Create a new DataClient
                client = new DataClient(config);
                client.Start();

                started = true;
            }
            else
            {
                // Throw exception that no configuration file was found
                var ex = new Exception("No Configuration File Found. Exiting TrakHound-DataClient!");
                log.Error(ex);
                throw ex;
            }
        }

        public static void Stop()
        {
            if (menuUpdateTimer != null)
            {
                menuUpdateTimer.Stop();
                menuUpdateTimer.Dispose();
            }

            if (client != null) client.Stop();

            started = false;
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

        private static void UpdateMenuStatus(object sender, ElapsedEventArgs e)
        {
            string status = started ? "Running" : "Stopped";
            WCF.MessageClient.Send("trakhound-dataclient-menu", new WCF.Message("Status", status));
        }
    }
}
