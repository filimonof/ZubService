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
    /// структура с данными подключения к SQL серверу Незабудки
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
        /// конструктор
        /// </summary>
        /// <param name="server">SQL сервер</param>
        /// <param name="database">база данных</param>
        /// <param name="winNTauth">windows аутентификация</param>
        /// <param name="login">логин</param>
        /// <param name="password">пароль</param>
        /// <param name="timeOut">время ожидания (сек)</param>
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
        /// строка подключения ксерверу SQL незабудки
        /// </summary>
        /// <returns>строка подключения</returns>
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
        /// чтение настроек
        /// </summary>
        /// <param name="prefix">префикс настроек</param>
        public void GetSettings(string prefix)
        {            
            this.Server = ConfigurationManager.AppSettings[prefix + "SQL сервер"];
            this.Database = ConfigurationManager.AppSettings[prefix + "база данных"];
            this.WinNTAuth = Convert.ToBoolean(ConfigurationManager.AppSettings[prefix + "WinNT аутентификация"]);
            this.Login = ConfigurationManager.AppSettings[prefix + "логин"];
            this.Password = ConfigurationManager.AppSettings[prefix + "пароль"];
            this.TimeOut = Convert.ToInt32(ConfigurationManager.AppSettings[prefix + "время ожидания (сек)"]);            
        }
    }

    /// <summary>
    /// Класс для работы с компелксом Незабудка
    /// </summary>
    public class Nezabudka
    {
        //по этой дате определяются новые данные или нет
        private DateTime lastDate = DateTime.MinValue;
        public DateTime LastDate
        {
            set { this.lastDate = value; }
            get { return this.lastDate; }
        }
        
        //два SQL сервера Незабудки
        private SqlConnection sqlConn1;
        private SqlConnection sqlConn2;

        //две структуры с парамтерами серверов Незабудки
        private NezabudkaStruct param1;
        private NezabudkaStruct param2;

        // делегат функция для вывода логов
        private WriteLogDelegate writer;

        private const string STR_NEW_DATA = 
            "SELECT StartTime, ChannelID, RemotePhoneNumber, Duration "
            + " FROM Calls "
            + " WHERE (StartTime > @StartTime)"
            + " ORDER BY StartTime ASC";

        private const string STR_GET_LAST_DATE = 
            "SELECT Max(StartTime) as MaxDate FROM Calls ";

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="p1">параметры первого сервера незабудки</param>
        /// <param name="p2">параметры второго сервера незабудки</param>
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
        /// открывается соединение с серверами незабудки
        /// </summary>
        /// <returns>true если хотя бы одно соединение есть</returns>
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
        /// определение текущего сервера Незабудки
        /// </summary>
        /// <returns>соединение с сервером незабудки</returns>
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
                    if (this.writer != null) writer("Переподключение к Незабудке-1.");
                }
                catch { resultCon = null; }
            }

            if (resultCon != null) return resultCon;

            if (this.sqlConn2.State == ConnectionState.Broken)
                this.sqlConn2.Close();
            if (this.sqlConn2.State == ConnectionState.Closed)
            {
                this.sqlConn2.Open();
                if (this.writer != null) writer("Переподключение к Незабудке-2.");                
            }

            return this.sqlConn2;
        }

        /// <summary>
        /// Определение последней записи, с неё и начинаем работать
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
        /// Получаем новые записи
        /// </summary>
        /// <returns>возвращаем ридер с данными</returns>
        public SqlDataReader GetDataToReader()
        {
            SqlCommand command = new SqlCommand(STR_NEW_DATA, this.CurrentConnection());
            command.Parameters.Add("@StartTime", SqlDbType.DateTime).Value = this.LastDate;
            return command.ExecuteReader(CommandBehavior.SingleResult);
        }

    }
}
