// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace TrakHound.DataClient
{
    static class Program
    {
        private const int ERROR_RETRY = 5000;

        private static Logger log = LogManager.GetCurrentClassLogger();
        private static ManualResetEvent stop;
        private static DataClient client;
        
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
                try
                {
                    // Start as Service
                    ServiceBase.Run(new ServiceBase[]
                    {
                    new DataClientService()
                    });
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    log.Info("TrakHound DataClient Error :: Restarting Server in 5 Seconds..");

                    if (!stop.WaitOne(ERROR_RETRY, true)) Init(args);
                }
            }
        }

        public static void Start()
        {
            // Get the default Configuration file path
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Configuration.FILENAME);

            // Create a new DataClient
            client = new DataClient(configPath);
            client.Start();
        }

        public static void Stop()
        {
            if (stop != null) stop.Set();

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
