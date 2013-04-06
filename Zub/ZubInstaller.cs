using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace vit.Zub
{
    /// <SUMMARY>
    /// ���������� (installer) ��� ������� Zub
    /// </SUMMARY> 
    [RunInstaller(true)]
    public partial class ZubInstaller : Installer
    {
        /// <SUMMARY>
        /// ����� �����������
        /// </SUMMARY> 
        public ZubInstaller()
        {
            InitializeComponent();
            ServiceProcessInstaller process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;                        

            ServiceInstaller serviceAdmin = new ServiceInstaller();
            serviceAdmin.StartType = ServiceStartMode.Automatic;
            serviceAdmin.ServiceName = "Zub";
            serviceAdmin.DisplayName = "Zub";
            serviceAdmin.Description = "������ ���������� ������� � ������ ����������� ���������� �� ��������� � ��������� CK-2003";

            // ������� ��������� ���������� � ������ ���������� 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {process, serviceAdmin} );
        }
    }
}
