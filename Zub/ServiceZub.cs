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
    /// ������� ��� ������ ��������� � ������ �������
    /// </summary>
    /// <param name="log">������ ���������</param>
    public delegate void WriteLogDelegate(string log);

    /// <summary>
    /// �������� ����� �������
    /// </summary>
    public partial class Zub : ServiceBase
    {
        // �������� �����
        protected Thread MainThread;

        // ������ ������� ������ (������� ��� �������� ������ �� ����� ������)
        protected ManualResetEvent ShutdownEvent;

        // ����������� � ���� ���������
        protected bool isLogFile = false;

        // �������� � ���������� ���������       
        protected TimeSpan Delay;
        
        // ����� ��������� � ��-2003
        protected int CK_EV_EventID = 0;
        
        // ����� ��������� � ��-2003
        protected int CK_EV_CategoryID = 0;

        // ����� ������ � ��-2003
        protected int CK_EV_LevelID = 0;
        
        // ���� ������ ��������� � �� (SQL+RTDB)
        //EV_WRITE_SQL   = $00000020;  // ������� ������������� � ���
        //EV_WRITE_RTDB  = $00000040;  // ������� ������ � ���� 
        protected int CK_EV_Flags = 0x60;

        // ����� ��� ������ � ����������
        protected Nezabudka Nezabud;

        //����� ��� ������ � ��
        protected CkGetData CK;

        //��� ���������� ����� ��� �����
        private readonly string LOG_FILE = "Zub.log";
       
        /// <summary>
        /// ���������� ��������� ������ �������
        /// </summary>
        public Zub()
        {
            InitializeComponent();
            LOG_FILE = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, LOG_FILE);
        }

        /// <summary>
        /// ������ ��������
        /// </summary>
        public void GetSettings()
        {            
            this.CK_EV_EventID = Convert.ToInt32(ConfigurationManager.AppSettings["��-2003 ����� ���������"]);
            this.CK_EV_CategoryID = Convert.ToInt32(ConfigurationManager.AppSettings["��-2003 ����� ���������"]);
            this.CK_EV_LevelID = Convert.ToInt32(ConfigurationManager.AppSettings["��-2003 ������� ���������"]);
            this.CK_EV_Flags = Convert.ToInt32(ConfigurationManager.AppSettings["��-2003 ���� ���������"]);
            this.isLogFile = Convert.ToBoolean(ConfigurationManager.AppSettings["����������� � ����"]);            
            int SecDelay = Convert.ToInt32(ConfigurationManager.AppSettings["�������� ��� ������ ��������� (���)"]);            
            if (SecDelay < 1) SecDelay = 1;
            // ������� ����� ������ timespan � ��������� �� ��������� 1 ���.
            this.Delay = new TimeSpan(0, 0, 0, SecDelay, 0);
        }
        
        /// <summary>
        /// ����� �������
        /// </summary>
        protected override void OnStart(string[] args)
        {           
            // ������ ������ ��� ������ � ����������
            NezabudkaStruct param1 = new NezabudkaStruct();
            NezabudkaStruct param2 = new NezabudkaStruct();
            try
            {
                WriteLog("������ �������� ������� ...");
                param1.GetSettings("���������-1 ");
                param2.GetSettings("���������-2 ");
                this.GetSettings();
            }
            catch (Exception e)
            {
                throw new Exception("������ ��� ������ �������� �� ����� ������������ \n" + e.ToString());
            }
            WriteLog("����������� � ���������...");
            Nezabud = new Nezabudka(param1, param2, new WriteLogDelegate(WriteLog));            
            if (!Nezabud.OpenConnect())
            {
                throw new Exception("��� ������ ������� �� ������� ������������ �� � ������ �� �������� ���������");
            }
            else
            {
                Nezabud.InitLastDate();
            } 

            //������ ������ ��� ������ � ��
            try
            {
                WriteLog("����������� � ��-2003...");
                CK = new CkGetData("Zub");
                if (CK.Connected) CK.CloseConnection();
                if (!CK.Connection())
                    WriteLog("��� ������ ������� �� ������� ������������ � ��-2003", EventLogEntryType.Error);
            }
            catch (Exception e)
            {
                throw new Exception("��� ������ ������� �� ������� ������������ � ��-2003 \n" + e.ToString());
            }

            // ������� ������ ������� ������ � �������������� ���
            this.ShutdownEvent = new ManualResetEvent(false);

            // ������� ������ threadstart ��� ServiceMain
            ThreadStart ts = new ThreadStart(this.ServiceMain);
            // ������� ������� �����
            MainThread = new Thread(ts);
            // ��������� �����
            MainThread.Start();

            // �������� ������� ������
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // �������� ������� ����� 
            base.OnStart(args);
        }

        /// <summary>
        /// ���� �������
        /// </summary>
        protected override void OnStop()
        {
            // ������ ������� - ���������
            // ����� ���� �� � ��� ������� MainThread.Interrupt()
            this.ShutdownEvent.Set();            

            // ����� ����� ��� ���������� � ������� 10 ���.
            MainThread.Join(10000);

            //��������
            CK = null;
            Nezabud = null;

            // �������� ������� ������
            GC.GetTotalMemory(true);

            //�������� ������� ����� 
            base.OnStop();
        }

        #region ����� ��������
        /*
        /// <summary>
        /// ��������������� �������
        /// </summary>        
        protected override void OnPause()
        {

            Thread.Sleep(Timeout.Infinite);
            //MainThread.Suspend();
            base.OnPause();
        }

        /// <summary>
        /// ������������� ������ �������
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
        /// �������� ������������ ���� ������� 
        /// </summary>
        protected void ServiceMain()
        {
            bool bSignaled = false;
            int nReturnCode = 0;

            while (true)
            {
                // ���� ����������� ������� ��� ��������� ��������
                bSignaled = this.ShutdownEvent.WaitOne(this.Delay, true);

                // ���� ������ ������ � ����������, ������� �� �����
                if (bSignaled == true)
                    break;

                try
                {
                    // �������� ���� ������ �������
                    nReturnCode = Execute();
                }
                catch (Exception e)
                {
                    WriteLog("������ � ������ ������� \n " + e.ToString(), EventLogEntryType.Error);                   
                }
            }
        }

        /// <summary>
        /// ���� ������ ������� (�������� �������)
        /// </summary>
        /// <RETURNS></RETURNS>
        protected virtual int Execute()
        {            
            SqlDataReader reader = Nezabud.GetDataToReader();
            try
            {
                while (reader != null && reader.Read())
                {
                    string s = "���������: ������ ��������� ���������� �� "
                        + ((DateTime)reader["StartTime"]).ToString()
                        + " �� ������ "
                        + reader["ChannelID"].ToString()
                        + " (" + ChannelToName((int)reader["ChannelID"]) + ") "
                        + (reader["RemotePhoneNumber"].ToString().Equals("") ? "" : " �������� ������� " + reader["RemotePhoneNumber"].ToString())
                        + ".";
                    if (!CK.Connected)
                    {
                        CK.Connection();
                        WriteLog("��������������� � ��-2003.");
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
        /// ����������� ���������� �� ������
        /// </summary>
        /// <param name="channel">����� ������</param>
        /// <returns>��� ����������</returns>
        private static string ChannelToName(int channel)
        {
            switch (channel)
            {
                case (01): return "������� ����������"; 
                case (03): return "��������"; 
                case (09): return "��������� ����� ����������"; 
                case (10): return "������� ���������"; 
                case (11): return "���������"; 
                default  : return "?";
            }
        }

        /// <summary>
        /// �������������� ���� ���� � ������� ��������� � �������� ������
        /// </summary>
        /// <param name="type">��� ��������� � ������� ���������</param>
        /// <returns>�������� ������</returns>
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
        /// ����� � ������ ��������� (Event Viewer)
        /// </summary>
        /// <param name="log">������ � ����������</param>
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
        /// ����� � ����, ���� ���������
        /// </summary>
        /// <param name="log">������ � ����������</param>
        /// <param name="param">��������</param>
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
//������ � ������ ������� 
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
