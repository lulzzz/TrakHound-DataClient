// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using WCF = TrakHound.Api.v2.WCF;
using TrakHound.DataClient.Messages;

namespace TrakHound.DataClient
{
    static class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static DataClient client;
        internal static ServiceHost MessageServer;

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
                ServiceBase.Run(new DataClientService());
            }
        }

        public static void Start()
        {
            // Start MessageServer
            if (MessageServer == null)
            {
                MessageServer = WCF.Server.Create<MessageServer>("trakhound-dataclient");
            }

            // Get the default Configuration file path
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);

            // Create a new DataClient
            client = new DataClient(configPath);
            client.Start();
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
