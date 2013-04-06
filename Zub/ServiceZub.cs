using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Configuration;
using vit.CK;

namespace vit.Zub
{
    /// <summary>
    /// делегат для вывода сообщений в журнал событий
    /// </summary>
    /// <param name="log">строка сообщения</param>
    public delegate void WriteLogDelegate(string log);

    /// <summary>
    /// Основной класс сервиса
    /// </summary>
    public partial class Zub : ServiceBase
    {
        // основной поток
        protected Thread MainThread;

        // ручное событие ресета (годится для удобства выхода из цикла потока)
        protected ManualResetEvent ShutdownEvent;

        // дублировать в файл сообщения
        protected bool isLogFile = false;

        // задержка в выполнении программы       
        protected TimeSpan Delay;
        
        // Номер сообщения в СК-2003
        protected int CK_EV_EventID = 0;
        
        // Номер категории в СК-2003
        protected int CK_EV_CategoryID = 0;

        // Номер уровня в СК-2003
        protected int CK_EV_LevelID = 0;
        
        // Флаг записи сообщения в СК (SQL+RTDB)
        //EV_WRITE_SQL   = $00000020;  // требует архивирования в РБД
        //EV_WRITE_RTDB  = $00000040;  // требует записи в БДРВ 
        protected int CK_EV_Flags = 0x60;

        // класс для работы с Незабудкой
        protected Nezabudka Nezabud;

        //класс для работы с СК
        protected CkGetData CK;

        //имя текстового файла для логов
        private readonly string LOG_FILE = "Zub.log";
       
        /// <summary>
        /// конструкор основного класса сервиса
        /// </summary>
        public Zub()
        {
            InitializeComponent();
            LOG_FILE = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, LOG_FILE);
        }

        /// <summary>
        /// чтение настроек
        /// </summary>
        public void GetSettings()
        {            
            this.CK_EV_EventID = Convert.ToInt32(ConfigurationManager.AppSettings["СК-2003 номер сообщения"]);
            this.CK_EV_CategoryID = Convert.ToInt32(ConfigurationManager.AppSettings["СК-2003 номер категории"]);
            this.CK_EV_LevelID = Convert.ToInt32(ConfigurationManager.AppSettings["СК-2003 уровень сообщения"]);
            this.CK_EV_Flags = Convert.ToInt32(ConfigurationManager.AppSettings["СК-2003 флаг сообщения"]);
            this.isLogFile = Convert.ToBoolean(ConfigurationManager.AppSettings["Дублировать в файл"]);            
            int SecDelay = Convert.ToInt32(ConfigurationManager.AppSettings["Задержка при опросе Незабудки (сек)"]);            
            if (SecDelay < 1) SecDelay = 1;
            // создаем новый объект timespan с задержкой по умолчанию 1 сек.
            this.Delay = new TimeSpan(0, 0, 0, SecDelay, 0);
        }
        
        /// <summary>
        /// Старт сервиса
        /// </summary>
        protected override void OnStart(string[] args)
        {           
            // создаём классы для работы с незабудкой
            NezabudkaStruct param1 = new NezabudkaStruct();
            NezabudkaStruct param2 = new NezabudkaStruct();
            try
            {
                WriteLog("Чтение настроек сервиса ...");
                param1.GetSettings("Незабудка-1 ");
                param2.GetSettings("Незабудка-2 ");
                this.GetSettings();
            }
            catch (Exception e)
            {
                throw new Exception("Ошибка при чтении настроек из файла конфигурации \n" + e.ToString());
            }
            WriteLog("Подключение к Незабудке...");
            Nezabud = new Nezabudka(param1, param2, new WriteLogDelegate(WriteLog));            
            if (!Nezabud.OpenConnect())
            {
                throw new Exception("При старте сервиса не удалось подключиться ни к одному из серверов Незабудки");
            }
            else
            {
                Nezabud.InitLastDate();
            } 

            //создаём классы для работы с СК
            try
            {
                WriteLog("Подключение к СК-2003...");
                CK = new CkGetData("Zub");
                if (CK.Connected) CK.CloseConnection();
                if (!CK.Connection())
                    WriteLog("При старте сервиса не удалось подключиться к СК-2003", EventLogEntryType.Error);
            }
            catch (Exception e)
            {
                throw new Exception("При старте сервиса не удалось подключиться к СК-2003 \n" + e.ToString());
            }

            // создаем ручное событие ресета и инициализируем его
            this.ShutdownEvent = new ManualResetEvent(false);

            // создаем объект threadstart для ServiceMain
            ThreadStart ts = new ThreadStart(this.ServiceMain);
            // создаем рабочий поток
            MainThread = new Thread(ts);
            // запускаем поток
            MainThread.Start();

            // Вызываем сборщик мусора
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // вызываем базовый класс 
            base.OnStart(args);
        }

        /// <summary>
        /// Стоп сервиса
        /// </summary>
        protected override void OnStop()
        {
            // сигнал событию - завершить
            // можно было бы и так сделать MainThread.Interrupt()
            this.ShutdownEvent.Set();            

            // ждать поток для завершения в течение 10 сек.
            MainThread.Join(10000);

            //зачистка
            CK = null;
            Nezabud = null;

            // Вызываем сборщик мусора
            GC.GetTotalMemory(true);

            //вызываем базовый класс 
            base.OnStop();
        }

        #region Паузу отключим
        /*
        /// <summary>
        /// Приостановление сервиса
        /// </summary>        
        protected override void OnPause()
        {

            Thread.Sleep(Timeout.Infinite);
            //MainThread.Suspend();
            base.OnPause();
        }

        /// <summary>
        /// Возобновление работы сервиса
        /// </summary>
        protected override void OnContinue()
        {
            MainThread.Interrupt();
            //MainThread.Resume();
            base.OnContinue();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnShutdown()
        {
            this.OnStop();
            base.OnShutdown();
        }
         */
        #endregion

        /// <summary> 
        /// Основной беЗконечнный цикл сервиса 
        /// </summary>
        protected void ServiceMain()
        {
            bool bSignaled = false;
            int nReturnCode = 0;

            while (true)
            {
                // ждем сигнального события или истечения задержки
                bSignaled = this.ShutdownEvent.WaitOne(this.Delay, true);

                // если пришел сигнал о завершении, выходим из цикла
                if (bSignaled == true)
                    break;

                try
                {
                    // выполним тело потока сервиса
                    nReturnCode = Execute();
                }
                catch (Exception e)
                {
                    WriteLog("Ошибка в работе сервиса \n " + e.ToString(), EventLogEntryType.Error);                   
                }
            }
        }

        /// <summary>
        /// тело потока сервиса (основная функция)
        /// </summary>
        /// <RETURNS></RETURNS>
        protected virtual int Execute()
        {            
            SqlDataReader reader = Nezabud.GetDataToReader();
            try
            {
                while (reader != null && reader.Read())
                {
                    string s = "НЕЗАБУДКА: запись разговора диспетчера от "
                        + ((DateTime)reader["StartTime"]).ToString()
                        + " по каналу "
                        + reader["ChannelID"].ToString()
                        + " (" + ChannelToName((int)reader["ChannelID"]) + ") "
                        + (reader["RemotePhoneNumber"].ToString().Equals("") ? "" : " удалённый телефон " + reader["RemotePhoneNumber"].ToString())
                        + ".";
                    if (!CK.Connected)
                    {
                        CK.Connection();
                        WriteLog("Переподключение к СК-2003.");
                    }
                    if (this.isLogFile) WriteFile(s, "EVN");
                    CK.SendEvent(s, null, DateTime.Now, this.CK_EV_EventID, this.CK_EV_CategoryID, this.CK_EV_LevelID, this.CK_EV_Flags);
                    Nezabud.LastDate = (DateTime)reader["StartTime"];
                }
            }
            finally
            {
                reader.Close();
                reader = null;                
            }
            return -1;
        }

        /// <summary>
        /// определение диспетчера по каналу
        /// </summary>
        /// <param name="channel">номер канала</param>
        /// <returns>имя диспетчера</returns>
        private static string ChannelToName(int channel)
        {
            switch (channel)
            {
                case (01): return "сотовый диспетчера"; 
                case (03): return "селектор"; 
                case (09): return "резервный пульт диспетчера"; 
                case (10): return "старший диспетчер"; 
                case (11): return "диспетчер"; 
                default  : return "?";
            }
        }

        /// <summary>
        /// преобразование типа лога в журнале сообщений в короткую строку
        /// </summary>
        /// <param name="type">тип сообщения в журнале сообщений</param>
        /// <returns>короткая строка</returns>
        private static string TypeToStr(EventLogEntryType type)
        {
            switch (type)
            {
                case (EventLogEntryType.Error): return "ERR";                
                case (EventLogEntryType.Information): return "INF";                
                case (EventLogEntryType.Warning): return "WAR";
                default: return "   ";
            }
        }

        /// <summary>
        /// вывод в журнал сообщений (Event Viewer)
        /// </summary>
        /// <param name="log">Строка с сообщением</param>
        private void WriteLog(string log)
        {
            WriteLog(log, EventLogEntryType.Information);            
        }

        private void WriteLog(string log, EventLogEntryType type)
        {
            try
            {
                EventLog.WriteEntry(log, type);
                if (this.isLogFile) WriteFile(log, TypeToStr(type));
            }
            catch { }
        }

        /// <summary>
        /// вывод в файл, если требуется
        /// </summary>
        /// <param name="log">строка с сообщением</param>
        /// <param name="param">параметр</param>
        private void WriteFile(string log, string param)
        {
            System.IO.StreamWriter sw = null;
            try
            {
                sw = System.IO.File.AppendText(LOG_FILE); 
                sw.WriteLine(string.Format("{0} \t {1} \t {2}", DateTime.Now, param, log));
            }
            catch{}
            finally
            {
                if (sw != null) sw.Close();                
            }
        }

        
    }
}


//Event Type:	Error
//Event Source:	Zub
//Event Category:	None
//Event ID:	0
//Date:		25.07.2007
//Time:		11:52:21
//User:		N/A
//Computer:	CK3
//Description:
//Ошибка в работе сервиса 
// System.InvalidOperationException: There is already an open DataReader associated with this Command which must be closed first.
//   at System.Data.SqlClient.SqlInternalConnectionTds.ValidateConnectionForExecute(SqlCommand command)
//   at System.Data.SqlClient.SqlConnection.ValidateConnectionForExecute(String method, SqlCommand command)
//   at System.Data.SqlClient.SqlCommand.ValidateCommand(String method, Boolean async)
//   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method, DbAsyncResult result)
//   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method)
//   at System.Data.SqlClient.SqlCommand.ExecuteReader(CommandBehavior behavior, String method)
//   at System.Data.SqlClient.SqlCommand.ExecuteReader()
//   at vit.Zub.Nezabudka.GetDataToReader()
//   at vit.Zub.Zub.Execute()
//   at vit.Zub.Zub.ServiceMain()

//For more information, see Help and Support Center at http://go.microsoft.com/fwlink/events.asp.
