/* 
 * Filimonov.Vitaliy@gmail.com 
 */
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace vit.Zub
{
    static class Program
    {
        /// <summary>
        /// Точка входа в прогамму
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;

            ServicesToRun = new ServiceBase[] { new Zub() };

            ServiceBase.Run(ServicesToRun);
        }
    }
}