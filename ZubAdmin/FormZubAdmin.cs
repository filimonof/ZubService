/* 
 * Filimonov.Vitaliy@gmail.com 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.ServiceProcess;

namespace vit.ZubAdmin
{

    public partial class FormZubAdmin : Form
    {

        Configuration cfg = null;

        ServiceController sc = null;

        TimeSpan timeout = new TimeSpan(0, 0, 30);

        public FormZubAdmin()
        {
            InitializeComponent();

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)            
                throw new PlatformNotSupportedException("Для работы нужна ОС Windows NT, 2000, XP или выше");

            try
            {
                sc = new ServiceController("Zub");
                RefreshService();
            }
            catch(Exception e)
            {
                MessageBox.Show("Возможно сервис не зарегистрирован в систем.е \n" + e.ToString());
            }
           
            //работа с конфигурационным файлом
            string cfgFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Zub.exe");
            if (System.IO.File.Exists(cfgFile))
            {
                cfg = ConfigurationManager.OpenExeConfiguration(cfgFile);
                if (!cfg.HasFile) CreateConfig();                
            }
            else
            {
                MessageBox.Show("Не найден сервис Zub.exe \n"
                    + "Данная программа по управлению сервисом должна быть в той же папке, что и сам сервис.\n"
                    + "Редактировать настройки не сможете.");
            }
            RefreshConfig();
            buttonRefreshConfig.Enabled = false;
            buttonSaveConfig.Enabled = false;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        //рабта с сервисом
        public void RefreshService()
        {
            if (sc.Status == ServiceControllerStatus.Running)
            {
                labelStatus.Text = "сервис работает";
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
                buttonRestart.Enabled = true;
            }
            else if (sc.Status == ServiceControllerStatus.Stopped)
            {
                labelStatus.Text = "сервис остановлен";
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
            }
            else if (sc.Status == ServiceControllerStatus.StartPending) 
            {
                labelStatus.Text = "сервис запускается";
                buttonStart.Enabled = false;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
            }
            else if (sc.Status == ServiceControllerStatus.StopPending)
            {
                labelStatus.Text = "сервис останавливается";
                buttonStart.Enabled = false;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
            }
        }

        private void buttonTestService_Click(object sender, EventArgs e)
        {
            try
            {
                ((Button)sender).Enabled = false;
                RefreshService();
            }
            finally
            {
                ((Button)sender).Enabled = true;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            sc.Start();
            try
            {
                sc.WaitForStatus(ServiceControllerStatus.Running, timeout);                
                RefreshService();
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                labelStatus.Text = "не удалось запустить сервис";
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (sc.CanStop)
            {
                labelStatus.Text = "остановка сервиса";
                sc.Stop();
                try
                {
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);                    
                    RefreshService();
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    labelStatus.Text = "не удалось остановить сервис";
                }

            }
            else
            {
                labelStatus.Text = "сервис не может быть остановлен";
            }
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            buttonStop_Click(sender, e);
            Application.DoEvents();
            buttonStart_Click(sender, e);
        }

        //работа с конфигурационным файлом
        public void CreateConfig()
        {
            cfg.AppSettings.Settings.Add("Задержка при опросе Незабудки (сек)","1");
            cfg.AppSettings.Settings.Add("Дублировать в файл", false.ToString());
            //CK
            cfg.AppSettings.Settings.Add("СК-2003 номер сообщения", "0");
            cfg.AppSettings.Settings.Add("СК-2003 номер категории", "0");
            cfg.AppSettings.Settings.Add("СК-2003 уровень сообщения", "0");
            cfg.AppSettings.Settings.Add("СК-2003 флаг сообщения", "0");
            //Nezabudka1
            cfg.AppSettings.Settings.Add("Незабудка-1 SQL Сервер", "");
            cfg.AppSettings.Settings.Add("Незабудка-1 база данных", "");
            cfg.AppSettings.Settings.Add("Незабудка-1 WinNT аутентификация",true.ToString());
            cfg.AppSettings.Settings.Add("Незабудка-1 логин", "");
            cfg.AppSettings.Settings.Add("Незабудка-1 пароль", "");
            cfg.AppSettings.Settings.Add("Незабудка-1 время ожидания (сек)", "0");
            //Nezabudka2
            cfg.AppSettings.Settings.Add("Незабудка-2 SQL Сервер", "");
            cfg.AppSettings.Settings.Add("Незабудка-2 база данных", "");
            cfg.AppSettings.Settings.Add("Незабудка-2 WinNT аутентификация", true.ToString());
            cfg.AppSettings.Settings.Add("Незабудка-2 логин", "");
            cfg.AppSettings.Settings.Add("Незабудка-2 пароль", "");
            cfg.AppSettings.Settings.Add("Незабудка-2 время ожидания (сек)", "0");
            cfg.Save();
        }

        public void SaveConfig()
        {
            cfg.AppSettings.Settings["Задержка при опросе Незабудки (сек)"].Value = numericDelay.Value.ToString();
            cfg.AppSettings.Settings["Дублировать в файл"].Value = checkIsDouplet.Checked.ToString();
            //CK
            cfg.AppSettings.Settings["СК-2003 номер сообщения"].Value = numericCKEventNum.Value.ToString();
            cfg.AppSettings.Settings["СК-2003 номер категории"].Value = numericCKCatNum.Value.ToString();
            cfg.AppSettings.Settings["СК-2003 уровень сообщения"].Value = numericCKLevelEvent.Value.ToString();
            cfg.AppSettings.Settings["СК-2003 флаг сообщения"].Value = numericCKFlagEvent.Value.ToString();
            //Nezabudka1
            cfg.AppSettings.Settings["Незабудка-1 SQL Сервер"].Value = textBoxSQL1.Text;
            cfg.AppSettings.Settings["Незабудка-1 база данных"].Value = textBoxDatabase1.Text;
            cfg.AppSettings.Settings["Незабудка-1 WinNT аутентификация"].Value = checkBoxWinNT1.Checked.ToString();
            cfg.AppSettings.Settings["Незабудка-1 логин"].Value = textBoxLogin1.Text;
            cfg.AppSettings.Settings["Незабудка-1 пароль"].Value = textBoxPass1.Text;
            cfg.AppSettings.Settings["Незабудка-1 время ожидания (сек)"].Value = numericTimeout1.Value.ToString();
            //Nezabudka2
            cfg.AppSettings.Settings["Незабудка-2 SQL Сервер"].Value = textBoxSQL2.Text;
            cfg.AppSettings.Settings["Незабудка-2 база данных"].Value = textBoxDatabase2.Text;
            cfg.AppSettings.Settings["Незабудка-2 WinNT аутентификация"].Value = checkBoxWinNT2.Checked.ToString();
            cfg.AppSettings.Settings["Незабудка-2 логин"].Value = textBoxLogin2.Text;
            cfg.AppSettings.Settings["Незабудка-2 пароль"].Value = textBoxPass2.Text;
            cfg.AppSettings.Settings["Незабудка-2 время ожидания (сек)"].Value = numericTimeout2.Value.ToString();
            cfg.Save();
        }

        public void RefreshConfig()
        {
            numericDelay.Value = GetOneSettings(cfg, "Задержка при опросе Незабудки (сек)", 1);
            checkIsDouplet.Checked = GetOneSettings(cfg, "Дублировать в файл", false);
            //CK
            numericCKEventNum.Value = GetOneSettings(cfg, "СК-2003 номер сообщения", 0);
            numericCKCatNum.Value = GetOneSettings(cfg, "СК-2003 номер категории", 0);
            numericCKLevelEvent.Value = GetOneSettings(cfg, "СК-2003 уровень сообщения", 0);
            numericCKFlagEvent.Value = GetOneSettings(cfg, "СК-2003 флаг сообщения", 0);
            //Nezabudka1
            textBoxSQL1.Text = GetOneSettings(cfg, "Незабудка-1 SQL Сервер", "");
            textBoxDatabase1.Text = GetOneSettings(cfg, "Незабудка-1 база данных", "");
            checkBoxWinNT1.Checked = GetOneSettings(cfg, "Незабудка-1 WinNT аутентификация", true);
            textBoxLogin1.Text = GetOneSettings(cfg, "Незабудка-1 логин", "");
            textBoxPass1.Text = GetOneSettings(cfg, "Незабудка-1 пароль", "");
            numericTimeout1.Value = GetOneSettings(cfg, "Незабудка-1 время ожидания (сек)", 0);
            //Nezabudka2
            textBoxSQL2.Text = GetOneSettings(cfg, "Незабудка-2 SQL Сервер", "");
            textBoxDatabase2.Text = GetOneSettings(cfg, "Незабудка-2 база данных", "");
            checkBoxWinNT2.Checked = GetOneSettings(cfg, "Незабудка-2 WinNT аутентификация", true);
            textBoxLogin2.Text = GetOneSettings(cfg, "Незабудка-2 логин", "");
            textBoxPass2.Text = GetOneSettings(cfg, "Незабудка-2 пароль", "");
            numericTimeout2.Value = GetOneSettings(cfg, "Незабудка-2 время ожидания (сек)", 0);
        }

        private static string GetOneSettings(Configuration cfg, string key, string @default)
        {
            string result;
            try { result = cfg.AppSettings.Settings[key].Value; }
            catch { result = @default; }            
            return result;
        }

        private static int GetOneSettings(Configuration cfg, string key, int @default)
        {
            int result;
            try { result = Convert.ToInt32(cfg.AppSettings.Settings[key].Value); }
            catch { result = @default; }            
            return result;
        }
        
        private static bool GetOneSettings(Configuration cfg, string key, bool @default)
        {
            bool result;
            try { result = Convert.ToBoolean(cfg.AppSettings.Settings[key].Value); }
            catch { result = @default; }
            return result;
        }

        private void buttonRefreshConfig_Click(object sender, EventArgs e)
        {
            RefreshConfig();
            buttonRefreshConfig.Enabled = false;
            buttonSaveConfig.Enabled = false;
        }

        private void buttonSaveConfig_Click(object sender, EventArgs e)
        {
            SaveConfig();
            buttonRefreshConfig.Enabled = false;
            buttonSaveConfig.Enabled = false;
        }

        private void editing_Changed(object sender, EventArgs e)
        {
            buttonRefreshConfig.Enabled = true;
            buttonSaveConfig.Enabled = true;
        }

    }
}