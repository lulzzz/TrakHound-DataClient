// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.ComponentModel;
using System.Configuration.Install;

namespace TrakHound.DataClient
{
    [RunInstaller(true)]
    public partial class DataClientProjectInstaller : Installer
    {
        public DataClientProjectInstaller()
        {
            InitializeComponent();
        }

        private void DataClientServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void DataClientProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
