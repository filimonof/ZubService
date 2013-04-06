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
                throw new PlatformNotSupportedException("��� ������ ����� �� Windows NT, 2000, XP ��� ����");

            try
            {
                sc = new ServiceController("Zub");
                RefreshService();
            }
            catch(Exception e)
            {
                MessageBox.Show("�������� ������ �� ��������������� � ������.� \n" + e.ToString());
            }
           
            //������ � ���������������� ������
            string cfgFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Zub.exe");
            if (System.IO.File.Exists(cfgFile))
            {
                cfg = ConfigurationManager.OpenExeConfiguration(cfgFile);
                if (!cfg.HasFile) CreateConfig();                
            }
            else
            {
                MessageBox.Show("�� ������ ������ Zub.exe \n"
                    + "������ ��������� �� ���������� �������� ������ ���� � ��� �� �����, ��� � ��� ������.\n"
                    + "������������� ��������� �� �������.");
            }
            RefreshConfig();
            buttonRefreshConfig.Enabled = false;
            buttonSaveConfig.Enabled = false;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        //����� � ��������
        public void RefreshService()
        {
            if (sc.Status == ServiceControllerStatus.Running)
            {
                labelStatus.Text = "������ ��������";
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
                buttonRestart.Enabled = true;
            }
            else if (sc.Status == ServiceControllerStatus.Stopped)
            {
                labelStatus.Text = "������ ����������";
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
            }
            else if (sc.Status == ServiceControllerStatus.StartPending) 
            {
                labelStatus.Text = "������ �����������";
                buttonStart.Enabled = false;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
            }
            else if (sc.Status == ServiceControllerStatus.StopPending)
            {
                labelStatus.Text = "������ ���������������";
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
                labelStatus.Text = "�� ������� ��������� ������";
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (sc.CanStop)
            {
                labelStatus.Text = "��������� �������";
                sc.Stop();
                try
                {
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);                    
                    RefreshService();
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    labelStatus.Text = "�� ������� ���������� ������";
                }

            }
            else
            {
                labelStatus.Text = "������ �� ����� ���� ����������";
            }
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            buttonStop_Click(sender, e);
            Application.DoEvents();
            buttonStart_Click(sender, e);
        }

        //������ � ���������������� ������
        public void CreateConfig()
        {
            cfg.AppSettings.Settings.Add("�������� ��� ������ ��������� (���)","1");
            cfg.AppSettings.Settings.Add("����������� � ����", false.ToString());
            //CK
            cfg.AppSettings.Settings.Add("��-2003 ����� ���������", "0");
            cfg.AppSettings.Settings.Add("��-2003 ����� ���������", "0");
            cfg.AppSettings.Settings.Add("��-2003 ������� ���������", "0");
            cfg.AppSettings.Settings.Add("��-2003 ���� ���������", "0");
            //Nezabudka1
            cfg.AppSettings.Settings.Add("���������-1 SQL ������", "");
            cfg.AppSettings.Settings.Add("���������-1 ���� ������", "");
            cfg.AppSettings.Settings.Add("���������-1 WinNT ��������������",true.ToString());
            cfg.AppSettings.Settings.Add("���������-1 �����", "");
            cfg.AppSettings.Settings.Add("���������-1 ������", "");
            cfg.AppSettings.Settings.Add("���������-1 ����� �������� (���)", "0");
            //Nezabudka2
            cfg.AppSettings.Settings.Add("���������-2 SQL ������", "");
            cfg.AppSettings.Settings.Add("���������-2 ���� ������", "");
            cfg.AppSettings.Settings.Add("���������-2 WinNT ��������������", true.ToString());
            cfg.AppSettings.Settings.Add("���������-2 �����", "");
            cfg.AppSettings.Settings.Add("���������-2 ������", "");
            cfg.AppSettings.Settings.Add("���������-2 ����� �������� (���)", "0");
            cfg.Save();
        }

        public void SaveConfig()
        {
            cfg.AppSettings.Settings["�������� ��� ������ ��������� (���)"].Value = numericDelay.Value.ToString();
            cfg.AppSettings.Settings["����������� � ����"].Value = checkIsDouplet.Checked.ToString();
            //CK
            cfg.AppSettings.Settings["��-2003 ����� ���������"].Value = numericCKEventNum.Value.ToString();
            cfg.AppSettings.Settings["��-2003 ����� ���������"].Value = numericCKCatNum.Value.ToString();
            cfg.AppSettings.Settings["��-2003 ������� ���������"].Value = numericCKLevelEvent.Value.ToString();
            cfg.AppSettings.Settings["��-2003 ���� ���������"].Value = numericCKFlagEvent.Value.ToString();
            //Nezabudka1
            cfg.AppSettings.Settings["���������-1 SQL ������"].Value = textBoxSQL1.Text;
            cfg.AppSettings.Settings["���������-1 ���� ������"].Value = textBoxDatabase1.Text;
            cfg.AppSettings.Settings["���������-1 WinNT ��������������"].Value = checkBoxWinNT1.Checked.ToString();
            cfg.AppSettings.Settings["���������-1 �����"].Value = textBoxLogin1.Text;
            cfg.AppSettings.Settings["���������-1 ������"].Value = textBoxPass1.Text;
            cfg.AppSettings.Settings["���������-1 ����� �������� (���)"].Value = numericTimeout1.Value.ToString();
            //Nezabudka2
            cfg.AppSettings.Settings["���������-2 SQL ������"].Value = textBoxSQL2.Text;
            cfg.AppSettings.Settings["���������-2 ���� ������"].Value = textBoxDatabase2.Text;
            cfg.AppSettings.Settings["���������-2 WinNT ��������������"].Value = checkBoxWinNT2.Checked.ToString();
            cfg.AppSettings.Settings["���������-2 �����"].Value = textBoxLogin2.Text;
            cfg.AppSettings.Settings["���������-2 ������"].Value = textBoxPass2.Text;
            cfg.AppSettings.Settings["���������-2 ����� �������� (���)"].Value = numericTimeout2.Value.ToString();
            cfg.Save();
        }

        public void RefreshConfig()
        {
            numericDelay.Value = GetOneSettings(cfg, "�������� ��� ������ ��������� (���)", 1);
            checkIsDouplet.Checked = GetOneSettings(cfg, "����������� � ����", false);
            //CK
            numericCKEventNum.Value = GetOneSettings(cfg, "��-2003 ����� ���������", 0);
            numericCKCatNum.Value = GetOneSettings(cfg, "��-2003 ����� ���������", 0);
            numericCKLevelEvent.Value = GetOneSettings(cfg, "��-2003 ������� ���������", 0);
            numericCKFlagEvent.Value = GetOneSettings(cfg, "��-2003 ���� ���������", 0);
            //Nezabudka1
            textBoxSQL1.Text = GetOneSettings(cfg, "���������-1 SQL ������", "");
            textBoxDatabase1.Text = GetOneSettings(cfg, "���������-1 ���� ������", "");
            checkBoxWinNT1.Checked = GetOneSettings(cfg, "���������-1 WinNT ��������������", true);
            textBoxLogin1.Text = GetOneSettings(cfg, "���������-1 �����", "");
            textBoxPass1.Text = GetOneSettings(cfg, "���������-1 ������", "");
            numericTimeout1.Value = GetOneSettings(cfg, "���������-1 ����� �������� (���)", 0);
            //Nezabudka2
            textBoxSQL2.Text = GetOneSettings(cfg, "���������-2 SQL ������", "");
            textBoxDatabase2.Text = GetOneSettings(cfg, "���������-2 ���� ������", "");
            checkBoxWinNT2.Checked = GetOneSettings(cfg, "���������-2 WinNT ��������������", true);
            textBoxLogin2.Text = GetOneSettings(cfg, "���������-2 �����", "");
            textBoxPass2.Text = GetOneSettings(cfg, "���������-2 ������", "");
            numericTimeout2.Value = GetOneSettings(cfg, "���������-2 ����� �������� (���)", 0);
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