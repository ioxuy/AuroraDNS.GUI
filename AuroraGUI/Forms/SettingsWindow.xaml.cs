﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using AuroraGUI.DnsSvr;
using AuroraGUI.Forms;
using AuroraGUI.Tools;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

// ReSharper disable LocalizableElement

namespace AuroraGUI
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            Log.IsChecked = DnsSettings.DebugLog;
            EDNSCustomize.IsChecked = DnsSettings.EDnsCustomize;
            WhiteList.IsChecked = DnsSettings.WhiteListEnable;
            DNSCache.IsChecked = DnsSettings.DnsCacheEnable;
            Proxy.IsChecked = DnsSettings.ProxyEnable;

            DoHUrlText.Text = DnsSettings.HttpsDnsUrl;
            SecondDoHUrlText.Text = DnsSettings.SecondHttpsDnsUrl;
            SecondDNS.Text =  DnsSettings.SecondDnsIp.ToString();
            EDNSClientIP.Text = DnsSettings.EDnsIp.ToString();
            ListenIP.Text = DnsSettings.ListenIp.ToString();

            AutoClean.IsChecked = DnsSettings.AutoCleanLogEnable;
            DNSMsg.IsChecked = DnsSettings.DnsMsgEnable;
            HTTP2Client.IsChecked = DnsSettings.Http2Enable;

            ProxyServer.Text = DnsSettings.WProxy.Address.Host;
            ProxyServerPort.Text = DnsSettings.WProxy.Address.Port.ToString();

            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                RunWithStart.IsChecked = MyTools.GetRunWithStart("AuroraDNS");
            else
                RunWithStart.IsEnabled = false;
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                RunAsAdmin.Visibility = Visibility.Visible;
            if (File.Exists($"{MainWindow.SetupBasePath}url.json"))
                L10N.Visibility = Visibility.Visible;
        }

        private void Proxy_OnChecked(object sender, RoutedEventArgs e)
        {
            ProxyServer.IsEnabled = true;
            ProxyServerPort.IsEnabled = true;
        }

        private void Proxy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ProxyServer.IsEnabled = false;
            ProxyServerPort.IsEnabled = false;
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            DnsSettings.DebugLog = Convert.ToBoolean(Log.IsChecked);
            DnsSettings.EDnsCustomize = Convert.ToBoolean(EDNSCustomize.IsChecked);
            DnsSettings.DnsCacheEnable = Convert.ToBoolean(DNSCache.IsChecked);
            DnsSettings.WhiteListEnable = Convert.ToBoolean(WhiteList.IsChecked);
            DnsSettings.ProxyEnable = Convert.ToBoolean(Proxy.IsChecked);
            DnsSettings.AutoCleanLogEnable = Convert.ToBoolean(AutoClean.IsChecked);
            DnsSettings.DnsMsgEnable = Convert.ToBoolean(DNSMsg.IsChecked);
            DnsSettings.Http2Enable = Convert.ToBoolean(HTTP2Client.IsChecked);

            if (!string.IsNullOrWhiteSpace(DoHUrlText.Text) &&
                !string.IsNullOrWhiteSpace(SecondDoHUrlText.Text) &&
                !string.IsNullOrWhiteSpace(SecondDNS.Text) &&
                !string.IsNullOrWhiteSpace(EDNSClientIP.Text) &&
                !string.IsNullOrWhiteSpace(ListenIP.Text))
            {
                DnsSettings.HttpsDnsUrl = DoHUrlText.Text.Trim();
                DnsSettings.SecondHttpsDnsUrl = SecondDoHUrlText.Text.Trim();
                DnsSettings.SecondDnsIp = IPAddress.Parse(SecondDNS.Text);
                DnsSettings.EDnsIp = IPAddress.Parse(EDNSClientIP.Text);
                DnsSettings.ListenIp = IPAddress.Parse(ListenIP.Text);

                if (Proxy.IsChecked == true)
                    DnsSettings.WProxy = new WebProxy(ProxyServer.Text + ":" + ProxyServerPort.Text);
                else
                    DnsSettings.WProxy = new WebProxy("127.0.0.1:80");

                if (DnsSettings.BlackListEnable && File.Exists("black.list"))
                    DnsSettings.ReadBlackList(MainWindow.SetupBasePath + "black.list");
                if (DnsSettings.ChinaListEnable && File.Exists("china.list"))
                    DnsSettings.ReadChinaList(MainWindow.SetupBasePath + "china.list");
                if (DnsSettings.WhiteListEnable && File.Exists("white.list"))
                    DnsSettings.ReadWhiteList(MainWindow.SetupBasePath + "white.list");
                if (DnsSettings.WhiteListEnable && File.Exists("rewrite.list"))
                    DnsSettings.ReadWhiteList(MainWindow.SetupBasePath + "rewrite.list");

                try
                {
                    File.WriteAllText($"{MainWindow.SetupBasePath}config.json",
                        "{\n" +
                        $"\"Listen\" : \"{DnsSettings.ListenIp}\",\n" +
                        $"\"SecondDns\" : \"{DnsSettings.SecondDnsIp}\",\n" +
                        $"\"BlackList\" : {DnsSettings.BlackListEnable.ToString().ToLower()},\n" +
                        $"\"ChinaList\" : {DnsSettings.ChinaListEnable.ToString().ToLower()},\n" +
                        $"\"RewriteList\" : {DnsSettings.WhiteListEnable.ToString().ToLower()},\n" +
                        $"\"DebugLog\" : {DnsSettings.DebugLog.ToString().ToLower()},\n" +
                        $"\"EDnsCustomize\" : {DnsSettings.EDnsCustomize.ToString().ToLower()},\n" +
                        $"\"EDnsClientIp\" : \"{DnsSettings.EDnsIp}\",\n" +
                        $"\"ProxyEnable\" : {DnsSettings.ProxyEnable.ToString().ToLower()},\n" +
                        $"\"HttpsDns\" : \"{DnsSettings.HttpsDnsUrl.Trim()}\",\n" +
                        $"\"SecondHttpsDns\" : \"{DnsSettings.SecondHttpsDnsUrl}\",\n" +
                        $"\"Proxy\" : \"{ProxyServer.Text + ":" + ProxyServerPort.Text}\",\n" +
                        $"\"EnableDnsCache\" : {DnsSettings.DnsCacheEnable.ToString().ToLower()},\n" +
                        $"\"EnableDnsMessage\" : {DnsSettings.DnsMsgEnable.ToString().ToLower()},\n" +
                        $"\"EnableAutoCleanLog\" : {DnsSettings.AutoCleanLogEnable.ToString().ToLower()},\n" +
                        $"\"EnableHttp2\" : {DnsSettings.Http2Enable.ToString().ToLower()} \n" +
                        "}");
                }
                catch (UnauthorizedAccessException exp)
                {
                    MessageBox.Show($"Error: 尝试写入配置文件权限不足，现在尝试以管理员权限启动。{Environment.NewLine}Original error: {exp}");
                    new MainWindow().RunAsAdmin();
                }
                catch (Exception exp)
                {
                    MessageBox.Show($"Error: 尝试写入配置文件错误{Environment.NewLine}Original error: {exp}");
                }

                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"设置已保存!" });
            }
            else
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"不应为空,请填写完全。" }); 
        }

        private void WhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "list files (*.list)|*.list|Hosts file|hosts|txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(File.ReadAllText(openFileDialog.FileName)))
                        Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"Error: 无效的空文件。" });
                    else
                    {
                        File.Copy(openFileDialog.FileName, $"{MainWindow.SetupBasePath}white.list");
                        Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"导入成功!" });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: 无法写入文件 {Environment.NewLine}Original error: " + ex.Message);
                }
            }

            if (File.Exists($"{MainWindow.SetupBasePath}white.list"))
                WhiteList.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> dohListStrings = null,dnsListStrings = null;
            var bgWorker = new BackgroundWorker();
            bgWorker.DoWork += (o, args) =>
            {
                try
                {
                    dohListStrings = new WebClient()
                        .DownloadString(UrlSettings.MDohList).Split('\n').ToList();
                    dnsListStrings = new WebClient()
                        .DownloadString(UrlSettings.MDnsList).Split('\n').ToList();

                    if (string.IsNullOrWhiteSpace(dohListStrings[dohListStrings.Count - 1]))
                        dohListStrings.RemoveAt(dohListStrings.Count - 1);
                    if (string.IsNullOrWhiteSpace(dnsListStrings[dnsListStrings.Count - 1]))
                        dnsListStrings.RemoveAt(dnsListStrings.Count - 1);
                }
                catch (Exception exception)
                {
                    MyTools.BackgroundLog(@"| Download list failed : " + exception);
                }
            };
            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                try
                {
                    if (dohListStrings != null && dohListStrings.Count != 0)
                        foreach (var item in dohListStrings)
                        {
                            DoHUrlText.Items.Add(item.Split('*', ',')[0].Trim());
                            SecondDoHUrlText.Items.Add(item.Split('*', ',')[0].Trim());
                        }
                    if (dnsListStrings != null && dnsListStrings.Count != 0)
                        foreach (var item in dnsListStrings)
                            SecondDNS.Items.Add(item.Split('*', ',')[0].Trim());

                    if (dohListStrings == null && dnsListStrings == null)
                        Snackbar.MessageQueue.Enqueue(new TextBlock() {Text = @"获取列表内容失败，请检查互联网连接。"});

                    if (File.Exists($"{MainWindow.SetupBasePath}doh.list"))
                        foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}doh.list"))
                        {
                            DoHUrlText.Items.Add(item.Split('*', ',')[0].Trim());
                            SecondDoHUrlText.Items.Add(item.Split('*', ',')[0].Trim());
                        }

                    if (File.Exists($"{MainWindow.SetupBasePath}dns.list"))
                        foreach (var item in File.ReadAllLines($"{MainWindow.SetupBasePath}dns.list"))
                            SecondDNS.Items.Add(item.Split('*', ',')[0].Trim());
                }
                catch (Exception exception)
                {
                    MyTools.BackgroundLog(@"| Read list failed : " + exception);
                }
            };
            bgWorker.RunWorkerAsync();
        }

        private void RunWithStart_Checked(object sender, RoutedEventArgs e) =>
            MyTools.SetRunWithStart(true, "AuroraDNS", GetType().Assembly.Location);

        private void RunWithStart_Unchecked(object sender, RoutedEventArgs e) =>
            MyTools.SetRunWithStart(false, "AuroraDNS", GetType().Assembly.Location);

        private void RunAsAdmin_OnClick(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = GetType().Assembly.Location,
                Verb = "runas"
            };
            try
            {
                Process.Start(startInfo);
                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception exception)
            {
                MyTools.BackgroundLog(exception.ToString());
            }
        }

        private void SpeedTestDoH_OnClick(object sender, RoutedEventArgs e)
        {
            new SpeedWindow().Show();
        }

        private void SpeedTestDNS_OnClick(object sender, RoutedEventArgs e)
        {
            new SpeedWindow(true).Show();
        }

        private void Expert_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{MainWindow.SetupBasePath}expert.json"))
                new ExpertWindow().Show();
            else
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"没有找到 expert.json 配置文件。" });
        }

        private void CleanCache_OnClick(object sender, RoutedEventArgs e)
        {
            Snackbar.IsActive = true;
            MemoryCache.Default.Trim(100);
            var snackbarMsg = new SnackbarMessage()
            {
                Content = "已经刷新内置缓存！",
                ActionContent = "刷新系统缓存"
            };
            snackbarMsg.ActionClick += (o, args) =>
            {
                new Process { StartInfo = new ProcessStartInfo("ipconfig.exe", "/flushdns") }.Start();
                Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"已刷新系统 DNS 解析缓存" });
            };
            Snackbar.Message = snackbarMsg;
        }

        private void ListL10N_OnClick(object sender, RoutedEventArgs e)
        {
            new ListL10NWindow().Show();
        }

        private void OpenList_OnClick(object sender, RoutedEventArgs e)
        {
            var snackbarMsg = new SnackbarMessage
            {
                Content = "没有找到白名单文件,是否创建?(格式与Hosts一致)",
                ActionContent = "现在创建并编辑"
            };
            snackbarMsg.ActionClick += (o, args) =>
            {
                File.Create($"{MainWindow.SetupBasePath}white.list").Close();
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}white.list"));
                Snackbar.IsActive = false;
            };
            Snackbar.Message = snackbarMsg;

            if (File.Exists($"{MainWindow.SetupBasePath}white.list"))
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}white.list"));
            else if (File.Exists($"{MainWindow.SetupBasePath}rewrite.list"))
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}rewrite.list"));
            else
                Snackbar.IsActive = true;
        }

        private void OpenSubList_OnClick(object sender, RoutedEventArgs e)
        {
            var snackbarMsg = new SnackbarMessage
            {
                Content = "没有找到订阅列表文件,是否创建?(每行一条地址)",
                ActionContent = "现在创建并编辑"
            };
            snackbarMsg.ActionClick += (o, args) =>
            {
                File.Create($"{MainWindow.SetupBasePath}white.sub.list").Close();
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}white.sub.list"));
                Snackbar.IsActive = false;
            };
            Snackbar.Message = snackbarMsg;

            if (File.Exists($"{MainWindow.SetupBasePath}white.sub.list"))
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}white.sub.list"));
            else if (File.Exists($"{MainWindow.SetupBasePath}rewrite.sub.list"))
                Process.Start(new ProcessStartInfo($"{MainWindow.SetupBasePath}rewrite.sub.list"));
            else
                Snackbar.IsActive = true;
        }

        private void CleanCacheSys_OnClick(object sender, RoutedEventArgs e)
        {
            new Process { StartInfo = new ProcessStartInfo("ipconfig.exe", "/flushdns") }.Start();
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"已刷新系统 DNS 解析缓存" });
        }

        private void CleanCacheInside_OnClick(object sender, RoutedEventArgs e)
        {
            MemoryCache.Default.Trim(100);
            Snackbar.MessageQueue.Enqueue(new TextBlock() { Text = @"已刷新内置 DNS 解析缓存" });
        }
    }
}
