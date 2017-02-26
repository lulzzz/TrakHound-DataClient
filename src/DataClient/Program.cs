// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace TrakHound.DataClient
{
    static class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static DataClient client;
        private static ServiceBase service;

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

                // Create a new DataClient
                client = new DataClient(config);
                client.Start();
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
            if (client != null) client.Stop();
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }
    }
}
