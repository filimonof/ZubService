using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace vit.Zub
{
    /// <SUMMARY>
    /// Установщик (installer) для сервиса Zub
    /// </SUMMARY> 
    [RunInstaller(true)]
    public partial class ZubInstaller : Installer
    {
        /// <SUMMARY>
        /// Класс установщика
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
            serviceAdmin.Description = "Сервис пересылает события о записи переговоров диспетчера из Незабудки в сообщения CK-2003";

            // добавим созданные инсталлеры к нашему контейнеру 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {process, serviceAdmin} );
        }
    }
}
