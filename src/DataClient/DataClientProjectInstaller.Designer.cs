namespace TrakHound.DataClient
{
    partial class DataClientProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DataClientProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DataClientServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DataClientProcessInstaller
            // 
            this.DataClientProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DataClientProcessInstaller.Password = null;
            this.DataClientProcessInstaller.Username = null;
            this.DataClientProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.DataClientProcessInstaller_AfterInstall);
            // 
            // DataClientServiceInstaller
            // 
            this.DataClientServiceInstaller.DisplayName = "TrakHound DataClient";
            this.DataClientServiceInstaller.ServiceName = "TrakHound-DataClient";
            this.DataClientServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.DataClientServiceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.DataClientServiceInstaller_AfterInstall);
            // 
            // DataClientProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DataClientProcessInstaller,
            this.DataClientServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DataClientProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DataClientServiceInstaller;
    }
}