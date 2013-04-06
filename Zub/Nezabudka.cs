/* 
 * Filimonov.Vitaliy@gmail.com 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace vit.Zub
{
    /// <summary>
    /// ��������� � ������� ����������� � SQL ������� ���������
    /// </summary>
    public struct NezabudkaStruct
    {
        public string Server;
        public string Database;
        public bool WinNTAuth;
        public string Login;
        public string Password;
        public int TimeOut;

        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="server">SQL ������</param>
        /// <param name="database">���� ������</param>
        /// <param name="winNTauth">windows ��������������</param>
        /// <param name="login">�����</param>
        /// <param name="password">������</param>
        /// <param name="timeOut">����� �������� (���)</param>
        public NezabudkaStruct(string server, string database, bool winNTauth, 
            string login, string password, int timeOut)
        {
            this.Server = server;
            this.Database = database;
            this.WinNTAuth = winNTauth;
            this.Login = login;
            this.Password = password;
            this.TimeOut = timeOut;
        }

        /// <summary>
        /// ������ ����������� �������� SQL ���������
        /// </summary>
        /// <returns>������ �����������</returns>
        public override string ToString()
        {
            string str = string.Format(" Persist Security Info=False; server={0}; database={1}; ", 
                this.Server, this.Database);
            if (!this.WinNTAuth)
                str += string.Format("user id={0}; password={1}; ", this.Login, this.Password);
            str += string.Format("Integrated Security={0}; connection timeout={1};  ", 
                (this.WinNTAuth ? "SSPI" : "false"), this.TimeOut);
            //if (this.NetworkLibrary != NetworkLibraryEnum.no)
            //    str += string.Format("Network Library={0}; ", dbm NetworkLibrary);
            return str;
        }

        /// <summary>
        /// ������ ��������
        /// </summary>
        /// <param name="prefix">������� ��������</param>
        public void GetSettings(string prefix)
        {            
            this.Server = ConfigurationManager.AppSettings[prefix + "SQL ������"];
            this.Database = ConfigurationManager.AppSettings[prefix + "���� ������"];
            this.WinNTAuth = Convert.ToBoolean(ConfigurationManager.AppSettings[prefix + "WinNT ��������������"]);
            this.Login = ConfigurationManager.AppSettings[prefix + "�����"];
            this.Password = ConfigurationManager.AppSettings[prefix + "������"];
            this.TimeOut = Convert.ToInt32(ConfigurationManager.AppSettings[prefix + "����� �������� (���)"]);            
        }
    }

    /// <summary>
    /// ����� ��� ������ � ���������� ���������
    /// </summary>
    public class Nezabudka
    {
        //�� ���� ���� ������������ ����� ������ ��� ���
        private DateTime lastDate = DateTime.MinValue;
        public DateTime LastDate
        {
            set { this.lastDate = value; }
            get { return this.lastDate; }
        }
        
        //��� SQL ������� ���������
        private SqlConnection sqlConn1;
        private SqlConnection sqlConn2;

        //��� ��������� � ����������� �������� ���������
        private NezabudkaStruct param1;
        private NezabudkaStruct param2;

        // ������� ������� ��� ������ �����
        private WriteLogDelegate writer;

        private const string STR_NEW_DATA = 
            "SELECT StartTime, ChannelID, RemotePhoneNumber, Duration "
            + " FROM Calls "
            + " WHERE (StartTime > @StartTime)"
            + " ORDER BY StartTime ASC";

        private const string STR_GET_LAST_DATE = 
            "SELECT Max(StartTime) as MaxDate FROM Calls ";

        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="p1">��������� ������� ������� ���������</param>
        /// <param name="p2">��������� ������� ������� ���������</param>
        public Nezabudka(NezabudkaStruct p1, NezabudkaStruct p2)
        {
            this.param1 = p1;
            this.param2 = p2;                       
            sqlConn1 = new SqlConnection(param1.ToString());
            sqlConn2 = new SqlConnection(param2.ToString());         
        }

        public Nezabudka(NezabudkaStruct p1, NezabudkaStruct p2, WriteLogDelegate writer)
            : this(p1, p2)
        {
            this.writer = writer;
        }        

        /// <summary>
        /// ����������� ���������� � ��������� ���������
        /// </summary>
        /// <returns>true ���� ���� �� ���� ���������� ����</returns>
        public bool OpenConnect()
        {               
            bool b1 = true;
            bool b2 = true;

            if (this.sqlConn1.State == ConnectionState.Broken)
                this.sqlConn1.Close();
            if (this.sqlConn1.State == ConnectionState.Closed)
            {
                try { this.sqlConn1.Open(); }
                catch { b1 = false; }
            }

            if (this.sqlConn2.State == ConnectionState.Broken)
                this.sqlConn2.Close();
            if (this.sqlConn2.State == ConnectionState.Closed)
            {
                try { this.sqlConn2.Open(); }
                catch { b2 = false; }
            }

            return b1 || b2;
        }

        /// <summary>
        /// ����������� �������� ������� ���������
        /// </summary>
        /// <returns>���������� � �������� ���������</returns>
        private SqlConnection CurrentConnection()
        {
            SqlConnection resultCon = this.sqlConn1;

            if (this.sqlConn1.State == ConnectionState.Broken)
                this.sqlConn1.Close();
            if (this.sqlConn1.State == ConnectionState.Closed)
            {
                try 
                { 
                    this.sqlConn1.Open();
                    if (this.writer != null) writer("��������������� � ���������-1.");
                }
                catch { resultCon = null; }
            }

            if (resultCon != null) return resultCon;

            if (this.sqlConn2.State == ConnectionState.Broken)
                this.sqlConn2.Close();
            if (this.sqlConn2.State == ConnectionState.Closed)
            {
                this.sqlConn2.Open();
                if (this.writer != null) writer("��������������� � ���������-2.");                
            }

            return this.sqlConn2;
        }

        /// <summary>
        /// ����������� ��������� ������, � �� � �������� ��������
        /// </summary>
        public void InitLastDate()
        {            
            if (this.LastDate == DateTime.MinValue)
            {
                SqlCommand command = new SqlCommand(STR_GET_LAST_DATE, this.CurrentConnection());
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                if (reader["MaxDate"] != null)
                    this.LastDate = (DateTime)reader["MaxDate"];
                reader.Close();
            }          
        }

        /// <summary>
        /// �������� ����� ������
        /// </summary>
        /// <returns>���������� ����� � �������</returns>
        public SqlDataReader GetDataToReader()
        {
            SqlCommand command = new SqlCommand(STR_NEW_DATA, this.CurrentConnection());
            command.Parameters.Add("@StartTime", SqlDbType.DateTime).Value = this.LastDate;
            return command.ExecuteReader(CommandBehavior.SingleResult);
        }

    }
}
