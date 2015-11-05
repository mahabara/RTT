﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;

using Ivi.Visa.Interop;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using TigerApplicationServiceClient;
using TSLControlClient.TslControl;
using Tiger.Ruma.WcfInterface;
using Tiger.Ruma;
using System.Reflection;
using System.ServiceModel;
using IronPython;
using Microsoft.Scripting;

using System.Web;
using System.Web.Services;
using System.Diagnostics;

namespace RTT
{
    public partial class MainForm : Form
    {
        //debug log
        private bool _debug = false;
        private bool _log = false;

        //serialport
        SerialPort _COM_RRU = new SerialPort();
        SerialPort _COM2 = new SerialPort();
        string _com2trans = "\r";
        //bool waitingForreceive = false;

        //lock
        private Object _lock_RRUCOM = new Object();
        private Object _lock_COM2 = new Object();
        private Object _lock_Instrument = new Object();

        //socket
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 8001);//定义一网络端点
        Socket newsock;//定义一个Socket
        const string socketNoresult = "RTT-ACK";
        const string socketNorru = "RRU is not connected.";
        const string socketNoserial2 = "Serial2 is not connected.";

        //rumaster
        ApplicationControl tas;
        TslControlClient tsl;
        IRumaControlClient rumaClient;
        Tiger.Ruma.IRumaCpriDataFlow icdf;
        string _cpriport;
        string[] selectedCpriPorts = new string[] { "1A", "1B" };

        int[] rxPortBuffer = new int[] { 256, 256 };

        int[] txIqBandWidth = new int[] { 64, 64 };

        int[] rxIqBandWidth = new int[] { 64, 64 };

        bool trigger1 = true;

        bool trigger2 = true;

        bool trigger3 = true;

        bool trigger4 = true;

        bool allocateAuxPort = true;

        bool allocateDebugPort = true;
        System.UInt32 totalRxBufferSize = 1024;
        System.UInt32 totalTxBufferSize = 1024;
        //visa
        bool VisaSwitch = true;//true ==visa32
        
        int sesnSA = -1, sesnSG = -1, sesnSG2 = -1, sesnRFBOX = -1, sesnRFBOX2 = -1, sesnIS = -1, sesnIS2 = -1, sesnDC5767A = -1;
        int viSA, viSG, viSG2, viRFBOX, viRFBOX2, viIS, viIS2, viDC5767A;

        //resourcemanager
        
        private Ivi.Visa.Interop.ResourceManager sa_rm;
        private Ivi.Visa.Interop.ResourceManager sg_rm;
        private Ivi.Visa.Interop.ResourceManager sg2_rm;
        private Ivi.Visa.Interop.ResourceManager rfbox_rm;
        private Ivi.Visa.Interop.ResourceManager rfbox2_rm;
        private Ivi.Visa.Interop.ResourceManager is_rm;
        private Ivi.Visa.Interop.ResourceManager is2_rm;
        private Ivi.Visa.Interop.ResourceManager dc5767a_rm;

        //io
        private FormattedIO488 sa_io;
        private FormattedIO488 sg_io;
        private FormattedIO488 sg2_io;
        private FormattedIO488 rfbox_io;
        private FormattedIO488 rfbox2_io;
        private FormattedIO488 is_io;
        private FormattedIO488 is2_io;
        private FormattedIO488 dc5767a_io;

        //session
        private Ivi.Visa.Interop.IMessage sa_sesn;
        private Ivi.Visa.Interop.IMessage sg_sesn;
        private Ivi.Visa.Interop.IMessage sg2_sesn;
        private Ivi.Visa.Interop.IMessage rfbox_sesn;
        private Ivi.Visa.Interop.IMessage rfbox2_sesn;
        private Ivi.Visa.Interop.IMessage is_sesn;
        private Ivi.Visa.Interop.IMessage is2_sesn;
        private Ivi.Visa.Interop.IMessage dc5767a_sesn;


        //backcolor
        private string backcolor = "";
        //forecolor
        private string forecolor = "";
        //font
        private string fontstyle = "";

        //TAB INDEX
        private int _tabindex = 1;
        //file name
        private string fName;

        
        
        public Address addr = new Address();
        //private long send_count;
        //private long receive_count;
        private StringBuilder builder = new StringBuilder(4096);
        private StringBuilder printbuilder = new StringBuilder(4096);
        private StringBuilder receivebuilder = new StringBuilder();
        private List<byte> buffer = new List<byte>(4096);
        private Links link;
        private bool Listening = false;//是否没有执行完invoke相关操作  
        private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke 
        private bool Closing_com2 = false;
        private byte[] binary_data_1 = new byte[9];//AA 44 05 01 02 03 04 05 EA
        
        //socket thread
        private static byte[] result = new byte[1024];
       // private static int myProt = Constant.HOSTPORT;   //端口  
        //static Socket serverSocket;
        private static bool _shouldStop=false;
        Thread _createServer;
        System.Net.Sockets.TcpListener listener;

      
        //System.Windows.Forms.Timer
        System.Windows.Forms.Timer socketprocesstimer = new System.Windows.Forms.Timer();
        System.Timers.Timer _evmtimer = new System.Timers.Timer();
        //System.Windows.Forms.Timer _evmtimer = new System.Windows.Forms.Timer();

        //tag of put to socket result buffer
        private bool socketTag = false;

        //socket result
        private StringBuilder socketbuilder = new StringBuilder();
        delegate void SetTextCallBack(string text);
        delegate void AddhistoryCallBack(string text);

        

        //use in button execute
        static List<string> _buttoncmd = new List<string>();
        Thread _buttoncmdthread;
        //use in ts execute
        static List<string> _tscmd = new List<string>();
        Thread _tsthread;

        //path
        const string _snapPath = @"c:\RTT\snapshot\";
        const string _rxevmPath = @"c:\RTT\rxevm\";
        const string backuppath = @".\backup\";
        const string updatePath = @".\update\";

        //textline
        int _textline = 0;

        public MainForm()
        {
            InitializeComponent();
            this.link = new Links();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {

            
            
            for (int i = 0; i < Constant.DEVICE_LIST.Length; i++)
            {

                this.comboBox_instrumentprefix.Items.Add(Constant.DEVICE_LIST[i]);
                

            }
            string localpath = Application.StartupPath;
            string filepath = localpath +@"\default.ts";
            if(File.Exists(filepath))
            {
                
                
                TsFileHelper tfh = new TsFileHelper();
                //get tabs from file
                List<Tabcontent> tabs = tfh.getTabs(filepath);
                //delete all current pages
                this.tabControl1.TabPages.Clear();
                //init tabindex
                this._tabindex = 0;
                //add tabpage from tabs
                if(tabs.Count!=0)
                {
                    foreach (Tabcontent tab in tabs)
                    {
                        this.AddTab(tab);
                    }
                }
                else
                {
                    Tabcontent tc = new Tabcontent();
                    this.AddTab(tc);
                }
                
            }
            else
            {
                string filePath = System.IO.Path.Combine(localpath, "default.ts");
                File.Create(filePath);
                Tabcontent tc = new Tabcontent();
                this.AddTab(tc);
            }


            
            //load config

            Dictionary<string, string> tempaddr = new Dictionary<string, string>();
            ConfigHelper ch = new ConfigHelper();
            tempaddr = ch.GetAddr();
            this.addr.SetAddress(tempaddr,this.link);


            //load color

            this.backcolor = ch.GetConfig("disp_backcolor");
            if (this.backcolor != "")
            {
                this.dataDisplayBox.BackColor = ColorTranslator.FromHtml(this.backcolor);
            }

            this.forecolor = ch.GetConfig("disp_forecolor");
            if (this.forecolor != "")
            {
                this.dataDisplayBox.ForeColor = ColorTranslator.FromHtml(this.forecolor);
            }

            //load font
            FontConverter fc = new FontConverter();
            this.fontstyle = ch.GetConfig("disp_font");
            if (this.fontstyle != "")
            {
                this.dataDisplayBox.Font = (Font)fc.ConvertFromString(this.fontstyle);
            }



            //init instrument status
            //initInstrumentStatus(this.addr, this.addr,true);
            this.initInstrumentStatusbyVisa32(this.addr, this.addr, true);
            foreach (Control ctl in this.SerialpropertyBox.Controls)
            {
                if (ctl.Name == "label_rruport")
                {
                    ctl.Text = this.addr.RRU;
                }
                else if (ctl.Name == "label_rrubaud")
                {
                    ctl.Text = this.addr.Baudrate_rru;
                }
                else if (ctl.Name == "label_serial2port")
                {
                    ctl.Text = this.addr.SERIAL2;
                }
                else if (ctl.Name == "label_serial2baud")
                {
                    ctl.Text = this.addr.Baudrate_com2;
                }
            }
            //log
            LogManager.LogFielPrefix = "RTT ";
            //string logPath = System.Environment.CurrentDirectory + @"\log\";
            string logPath = @"c:\RTT\log\";
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
            LogManager.LogPath = logPath;
            LogManager.WriteLog(LogFile.Trace, "RTT start, version is "+ Assembly.GetExecutingAssembly().GetName().Version.ToString());

            //update
            
            if (!Directory.Exists(updatePath))
                Directory.CreateDirectory(updatePath);
            
            if (!Directory.Exists(backuppath))
                Directory.CreateDirectory(backuppath);
            //snapshot
            //_snapPath = System.Environment.CurrentDirectory + @"\snapshot\";

            if (!Directory.Exists(_snapPath))
                Directory.CreateDirectory(_snapPath);

            //rxevm
             
            if (!Directory.Exists(_rxevmPath))
                Directory.CreateDirectory(_rxevmPath);

            this.Text = "RTT v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Show();
            this.InputBox.Focus();
            
        }

        //modify current tab' name
        //add a new tab
        private void AddTab(Tabcontent tc)
        {

            NewTabPage Page;
            TableLayoutPanel layoutPanel = new TableLayoutPanel();
            layoutPanel.RowCount = 4;
            layoutPanel.ColumnCount = 8;
            layoutPanel.Dock = DockStyle.Fill;
            //tc has no buttons,add a empty page
            if (tc.tabname == "")
            {
                Page = new NewTabPage();
                Page.Name = "Page" + _tabindex.ToString();
                Page.Text = "tabPage" + _tabindex.ToString();
                Page.TabIndex = _tabindex;
                Page.Controls.Add(layoutPanel);

                this.tabControl1.Controls.Add(Page);
                //layout button
                //int x = 8, y = 10;
                for (int i = 0; i != 32; i++)
                {

                    TabButton tb = new TabButton();
                    tb._index = i;
                    tb.Dock = DockStyle.Fill;
                    toolTip1.SetToolTip(tb, tb.Text);
                    /*Page.Controls.Add(tb);
                    tb.Location = new System.Drawing.Point(x, y);
                    x = x + tb.Width + 7;
                    if (x + tb.Width > Page.Width)
                    {
                        x = 8;
                        y = y + tb.Height + 6;
                    }*/
                    for (int k = 0; k < layoutPanel.RowCount; k++)
                    {
                        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                        for (int j = 0; j < layoutPanel.ColumnCount; j++)
                        {
                            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                            layoutPanel.Controls.Add(tb);
                        }
                    }
                    tb.Click += new System.EventHandler(command_button_Click);
                    tb.MouseDown += new MouseEventHandler(command_button_MouseDown);
                }
            }
            //tc has buttons
            else
            {
                Page = new NewTabPage();
                Page.Name = tc.tabname;
                Page.Text = tc.tabname;
                Page.TabIndex = _tabindex;
                Page.Controls.Add(layoutPanel);
                this.tabControl1.Controls.Add(Page);
                //layout button
                //int x = 8, y = 10;
                for (int i = 0; i != 32; i++)
                {

                    TabButton tb = new TabButton();
                    tb._data = tc.buttons[i].data;
                    tb._index = i;
                    tb.Name = tc.buttons[i].btnname;
                    tb.Text = tc.buttons[i].btnname;
                    tb.Dock = DockStyle.Fill;
                    toolTip1.SetToolTip(tb, tb.Text);
                    /*Page.Controls.Add(tb);
                    tb.Location = new System.Drawing.Point(x, y);
                    x = x + tb.Width + 7;
                    if (x + tb.Width > Page.Width)
                    {
                        x = 8;
                        y = y + tb.Height + 6;
                    }*/
                    for (int k = 0; k < layoutPanel.RowCount; k++)
                    {
                        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                        for (int j = 0; j < layoutPanel.ColumnCount; j++)
                        {
                            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                            layoutPanel.Controls.Add(tb);
                        }
                    }
                    tb.Click += new System.EventHandler(command_button_Click);
                    tb.MouseDown += new MouseEventHandler(command_button_MouseDown);
                }
            }




            #region 三种设置某个选项卡为当前选项卡的方法
            //this.tabControl1.SelectedIndex = index; 
            //this.tabControl1.SelectedTab = Page;
            //this.tabControl1.SelectTab("Page" + index.ToString()); 
            #endregion

            _tabindex++;
        }




        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void button_save_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            ConfigHelper ch = new ConfigHelper();
            string defaultfilename = ch.GetConfig(Constant.CONFIG_TS_LOAD_PATH);
            sfd.InitialDirectory = defaultfilename;
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                fName = sfd.FileName;

                //this.dataDisplayBox.AppendText(fName + "\n");
                //init List<Tabcontent> tabs

                //get current tabs in mainform
                List<Tabcontent> tabs = GetCurrentTabs();

               
                TsFileHelper tfh = new TsFileHelper();
                tfh.SaveTab(tabs, fName);



            }
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ConfigHelper ch = new ConfigHelper();
            string defaultfilename = ch.GetConfig(Constant.CONFIG_TS_LOAD_PATH);
            ofd.InitialDirectory = defaultfilename;
            ofd.RestoreDirectory = true;
            
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fName = ofd.FileName;
                
                //this.dataDisplayBox.AppendText(fName + "\n");
                string fpath = fName.Replace(ofd.SafeFileName, "");
                //this.dataDisplayBox.AppendText(fpath + "\n");
                TsFileHelper tfh = new TsFileHelper();
                //get tabs from file
                List<Tabcontent> tabs = tfh.getTabs(fName);
                //delete all current pages
                this.tabControl1.TabPages.Clear();
                //init tabindex
                this._tabindex = 0;
                //add tabpage from tabs
                foreach(Tabcontent tab in tabs)
                {
                    this.AddTab(tab);
                }
                ch.UpdateConfig(Constant.CONFIG_TS_LOAD_PATH, fpath);

            }
        }

        private void button_lock_Click(object sender, EventArgs e)
        {
            if(this.button_lock.Text == "Lock")
            {
                this.tabControl1.Enabled = false;
                this.button_lock.Text = "Unlock";
            }
            else
            {
                this.tabControl1.Enabled = true;
                this.button_lock.Text = "Lock";
            }
            
        }
        //rru serialport open ,and listen to datareceive when checked state, close when unchecked state
        private void Rru_con_Button_Click(object sender, EventArgs e)
        {
            //rru serialport open ,and listen to datareceive
            if (this.rruConnButton.Checked == false)
            {
                //判断串口是否初始化
                //if (this._COM_RRU == null)
                //{
                    //初始化串口，如果未设置串口则弹出对话框提示用户先设置串口
                    if (this.addr.RRU == "")
                    {
                        WriteTraceText("Please setup serial port first!");
                        
                    }
                    else
                    {
                        //设定port,波特率,无检验位，8个数据位，1个停止位
                        this._COM_RRU = new SerialPort(this.addr.RRU, int.Parse(this.addr.Baudrate_rru), Parity.None, 8, StopBits.One);
                        this._COM_RRU.ReadBufferSize = 2048;
                        this._COM_RRU.ReceivedBytesThreshold = 1;
                        this._COM_RRU.NewLine = "\n";
                        this._COM_RRU.DtrEnable = true;
                        this._COM_RRU.RtsEnable = true;
                        this._COM_RRU.ReadTimeout = 3000;
                        this._COM_RRU.WriteTimeout = 3000;
                        

                    //open serial port

                    try
                        {
                                
                            this._COM_RRU.Open();
                            this._COM_RRU.DataReceived += Com_rru_DataReceived;
                            this._COM_RRU.ErrorReceived += Com_rru_ErrorReceived;
                            WriteTraceText("rru serialport open ,and listen to datareceive.");
                            
                            this.rruConnButton.CheckState = CheckState.Checked;
                        }
                        catch (Exception ex)
                        {
                            this._COM_RRU = null;
                            //现实异常信息给客户。  
                            WriteErrorText("serial port rru open failed: "+ex.Message);
                        }
                        
                    }
                    


               // }

                //open serial port
               /* else if (this._COM_RRU.IsOpen != true)
                {
                    //open serial port

                    try
                    {
                        this._COM_RRU.PortName = this.addr.RRU;
                        this._COM_RRU.BaudRate = int.Parse(this.addr.Baudrate_rru);
                        this._COM_RRU.Parity = Parity.None;
                        this._COM_RRU.DataBits = 8;
                        this._COM_RRU.StopBits = StopBits.One;

                        this._COM_RRU.Open();

                        SetText("rru serialport open ,and listen to datareceive.");
                        LogManager.WriteLog(LogFile.Trace, "rru serialport open ,and listen to datareceive.");
                        this.rruConnButton.CheckState = CheckState.Checked;
                    }
                    catch (Exception ex)
                    {
                        this._COM_RRU = null;
                        //现实异常信息给客户。  
                        MessageBox.Show(ex.Message);
                    }
                }*/
                


            }
            //close serial port
            else
            {
                Close_serial_port();
                this.rruConnButton.CheckState = CheckState.Unchecked;
            }

        }
        private void Close_serial2_port()
        {
            Closing_com2 = true;
            //string command = "";
            //this.command_Process(command);
            //while (Listening) Application.DoEvents();
            
            this._COM2.Close();
            Closing_com2 = false;
            WriteTraceText("close serial 2 port.");
            
        }

        //close serial port
        private void Close_serial_port()
        {
            Closing = true;
            //string command = "";
            //this.command_Process(command);
            while (Listening) Application.DoEvents();
            
            this._COM_RRU.Close();
            Closing = false;
            WriteTraceText("rru serial port closed.");
            
        }

        //error data receive
        private void Com2_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            WriteTraceText(sender.ToString() + " : " + e.ToString());
            
            this._COM2.DiscardInBuffer();
            this._COM2.DiscardOutBuffer();

        }

        private void Com_2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(200);
            if (Closing_com2) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环  
            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。  
                //int n = this.link.com_rru.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
                string tempstr = this._COM2.ReadExisting();
                if (tempstr.Trim().Length != 0)
                {
                    if (checkBox_pause.Checked == false)
                        this.Invoke((EventHandler)(delegate 
                        {
                            
                            this.dataDisplayBox.AppendText(tempstr);
                            if (this.checkBox_AutoscrollDown.Checked)
                            {
                                this.dataDisplayBox.ScrollToCaret();
                            }
                            

                        }));
                    
                    
                }
                
                


            }
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。  
            }
        }

        //error data receive
        private void Com_rru_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            WriteTraceText(sender.ToString() + " : " + e.ToString());
            
            this._COM_RRU.DiscardInBuffer();
            this._COM_RRU.DiscardOutBuffer();
            
        }
        //serial rru datareceive
        private void Com_rru_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string tempstr;
            if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环  
            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。
                byte[] readbuf = new byte[this._COM_RRU.BytesToRead];
                char[] buf;
                int n = 0;
                lock (_lock_RRUCOM)
                {
                    //tempstr = this._COM_RRU.ReadExisting();
                    n = this._COM_RRU.Read(readbuf, 0, readbuf.Length);
                    buf = Encoding.ASCII.GetChars(readbuf);
                    if (n>0)
                    {
                        for(int i=0; i!= buf.Length; i++)
                        {
                            if(buf[i]=='\n'||buf[i]=='\r')
                            {
                                if(buf[i]=='\n')
                                {
                                    //builder.Append("\n");
                                    tempstr = builder.ToString();
                                    //if(tempstr.Trim(' ').Trim('\n').Length !=0)
                                    WriteTraceText(tempstr);
                                    if (this.socketTag == true)
                                    {
                                        this.socketbuilder.Append(tempstr);

                                    }
                                    builder.Clear();
                                }
                            }
                            else
                            {
                                builder.Append(buf[i]);
                            }
                        }
                        //tempstr = Encoding.ASCII.GetString(readbuf);
                        //WriteText(tempstr);
                        //builder.Append(tempstr);
                        //if (tempstr.Contains('$'))
                        //{
                        //    WriteTraceNoText(builder.ToString());
                        //    builder.Clear();
                        //}

                        //if (this.socketTag == true)
                        //{
                        //    this.socketbuilder.Append(tempstr);

                        //}
                    }
                }


            }
            catch(Exception exp)
            {
                WriteErrorText("COM_RRU receive error: "+exp.Message);
                
            }    
            
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。  
            }
        }
        /*
        private void Com_rru_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(200);
            if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环  
            try  
            {  
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。  
                int n = this.link.com_rru.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据  
                //received_count += n;//增加接收计数  
                this.link.com_rru.Read(buf, 0, n);//读取缓冲数据 
                this.link.com_rru.DiscardInBuffer();
                //this.dataDisplayBox.AppendText(n.ToString()+"\n");
                //this.dataDisplayBox.AppendText(Encoding.ASCII.GetString(buf) + "\n");
                //this.dataDisplayBox.AppendText("11111111111\n");
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////  
                //<协议解析>  
                bool data_1_catched = false;//缓存记录数据是否捕获到  
                                            //1.缓存数据  
                buffer.AddRange(buf);  
                //2.完整性判断  
                while (buffer.Count >= 4)//至少要包含头（2字节）+长度（1字节）+校验（1字节）  
                {  
                    //请不要担心使用>=，因为>=已经和>,<,=一样，是独立操作符，并不是解析成>和=2个符号  
                    //2.1 查找数据头  
                    if (buffer[0] == 0xAA && buffer[1] == 0x44)  
                    {  
                        //2.2 探测缓存数据是否有一条数据的字节，如果不够，就不用费劲的做其他验证了  
                        //前面已经限定了剩余长度>=4，那我们这里一定能访问到buffer[2]这个长度  
                        int len = buffer[2];//数据长度  
                        //数据完整判断第一步，长度是否足够  
                        //len是数据段长度,4个字节是while行注释的3部分长度  
                        if (buffer.Count<len + 4) break;//数据不够的时候什么都不做  
                        //这里确保数据长度足够，数据头标志找到，我们开始计算校验  
                        //2.3 校验数据，确认数据正确  
                        //异或校验，逐个字节异或得到校验码  
                        byte checksum = 0;  
                        for (int i = 0; i<len + 3; i++)//len+3表示校验之前的位置  
                        {  
                            checksum ^= buffer[i];  
                        }  
                        if (checksum != buffer[len + 3]) //如果数据校验失败，丢弃这一包数据  
                        {  
                            buffer.RemoveRange(0, len + 4);//从缓存中删除错误数据  
                            continue;//继续下一次循环  
                        }
//至此，已经被找到了一条完整数据。我们将数据直接分析，或是缓存起来一起分析  
//我们这里采用的办法是缓存一次，好处就是如果你某种原因，数据堆积在缓存buffer中  
//已经很多了，那你需要循环的找到最后一组，只分析最新数据，过往数据你已经处理不及时  
//了，就不要浪费更多时间了，这也是考虑到系统负载能够降低。  
                        buffer.CopyTo(0, binary_data_1, 0, len + 4);//复制一条完整数据到具体的数据缓存  
                        data_1_catched = true;  
                        buffer.RemoveRange(0, len + 4);//正确分析一条数据，从缓存中移除数据。  
                    }  
                    else  
                    {  
                        //这里是很重要的，如果数据开始不是头，则删除数据  
                        buffer.RemoveAt(0);  
                    }  
                }  
                //分析数据  
                if (data_1_catched)  
                {  
                    //我们的数据都是定好格式的，所以当我们找到分析出的数据1，就知道固定位置一定是这些数据，我们只要显示就可以了  
                    string data = binary_data_1[3].ToString("X2") + " " + binary_data_1[4].ToString("X2") + " " +
                        binary_data_1[5].ToString("X2") + " " + binary_data_1[6].ToString("X2") + " " +
                        binary_data_1[7].ToString("X2");
                    //更新界面  
                    //this.Invoke((EventHandler)(delegate { this.dataDisplayBox.Text = data; }));  
                }  
                //如果需要别的协议，只要扩展这个data_n_catched就可以了。往往我们协议多的情况下，还会包含数据编号，给来的数据进行  
                //编号，协议优化后就是： 头+编号+长度+数据+校验  
                //</协议解析>  
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////  
                builder.Clear();//清除字符串构造器的内容  
                //因为要访问ui资源，所以需要使用invoke方式同步ui。  
                this.Invoke((EventHandler)(delegate
                {  
                    //判断是否是显示为16禁止  
                    if (checkBoxHexView.Checked)  
                    {  
                        //依次的拼接出16进制字符串  
                        foreach (byte b in buf)  
                        {  
                            builder.Append(b.ToString("X2") + " ");  
                        }  
                    }  
                    else  
                    {
                        //依次判断段落结尾，默认为\n
                        for(int i =0; i!=buf.Length;i++)
                        {
                            if (buf[i] != System.BitConverter.GetBytes(10)[0])
                            {
                                byte[] tempbytes = new byte[1];
                                tempbytes[0] = buf[i];
                                builder.Append(Encoding.ASCII.GetString(tempbytes));
                            }
                                
                            else
                            {
                                string tempstr = builder.ToString();
                                if (tempstr.Trim().Length != 0)
                                {
                                    this.dataDisplayBox.AppendText(tempstr + "\n");
                                    LogManager.WriteLog(LogFile.Trace, tempstr);
                                    //append to socketbuilder
                                    if (this.socketTag == true)
                                        socketbuilder.Append(tempstr + "\n");
                                    builder.Clear();
                                }

                                
                            }
                        }
                        
                        //直接按ASCII规则转换成字符串  
                        
                        //receivebuilder.Append(Encoding.ASCII.GetString(buf));
                        //append to socketbuilder
                        //if (this.socketTag==true)
                            //socketbuilder.Append(Encoding.ASCII.GetString(buf));
                    }
                    //追加的形式添加到文本框末端，并滚动到最后。 
                    //SetText(builder.ToString());
                    //this.dataDisplayBox.AppendText(builder.ToString());
                    
                    
                    if (receivebuilder.ToString().Contains('$'))
                    {
                        LogManager.WriteLog(LogFile.Trace, (receivebuilder.ToString()));
                        receivebuilder.Clear();
                    }
                    else if(receivebuilder.Length>1024)
                    {
                        LogManager.WriteLog(LogFile.Trace, (receivebuilder.ToString()));
                        receivebuilder.Clear();
                    }
                    
                    
                    
//修改接收计数  
//labelGetCount.Text = "Get:" + received_count.ToString();
}));  
            }  
            finally  
            {  
                Listening = false;//我用完了，ui可以关闭串口了。  
            }  
        }

    */
        //send button
        private void button_sendcommand_Click(object sender, EventArgs e)
        {
            
            string command = this.InputBox.Text;
            this.command_Process(command);
               
            

        }

        
        //command from socket
        private string SocketCommandProcess(string command)
        {
            

            string result = " ";
            //cmd$delay\0
            if (command != null)
            {

                
                LogManager.WriteLog(LogFile.Debug, "socket receive:"+command);
                string[] scokcmd = command.Split('$');
                if(scokcmd.Length==2)
                    result = this.command_Process(scokcmd[0], int.Parse(scokcmd[1]));
                else if(scokcmd.Length == 3)
                    result = this.command_Process(scokcmd[0], int.Parse(scokcmd[1]), scokcmd[2]);
            }
            
            return result;
        }

        //in visacom mode
        private void SaCapturebyVisacom(string name = "")
        {
            
            StringBuilder filename = new StringBuilder();
            string cmd = "MMEM:STOR:SCR \"D:\\rttscr.png\"";//;*OPC?
            this.sa_sesn.WriteString(cmd);
            this.sa_sesn.WriteString("MMEM:DATA? \"D:\\rttscr.png\"");
            if (name != "")
            {
                filename.Append(name).Append(@".png");
            }
            else
            {
                filename.Append(DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss")).Append(@".png");
            }
            //this.sa_sesn.WriteString("CALCulate:DATA?");
            
            byte[] readbuf;
            
            readbuf = this.sa_sesn.Read(1000000);
            //WriteDebugText(Encoding.ASCII.GetString(readbuf));
            Thread.Sleep(4000);
            this.sa_sesn.WriteString("MMEM:DEL \"D:\\rttscr.png\"");
            this.sa_sesn.WriteString("*CLS");
            
            byte[] size = { readbuf[1] };
            
            byte[] newbuf = readbuf.Skip(2+ int.Parse(Encoding.ASCII.GetString(size))).Take(readbuf.Length-9).ToArray();
            
            File.WriteAllBytes(_snapPath + filename, newbuf);
        }

        //only in visa32 mode
        private void SaCapture(string name="")
        {
            // SA截图
            int status = -1;
            StringBuilder filename = new StringBuilder();
            byte[] buf = new byte[1];
            
            string cmd = "MMEM:STOR:SCR \"D:\\rttscr.png\"";//;*OPC?
            buf = Encoding.ASCII.GetBytes(cmd);
            
            int recount;
            visa32.viWrite(viSA, buf, buf.Length, out recount);
            
            buf = Encoding.ASCII.GetBytes("MMEM:DATA? \"D:\\rttscr.png\"\n");
            visa32.viWrite(viSA, buf, buf.Length, out recount);
            byte[] readbuf = new byte[1000000];
            if (name != "")
            {
                filename.Append(name).Append(@".png");
            } 
            else
            {
                filename.Append(DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss")).Append(@".png");
            }
            
            
            int tmep = readbuf.Length;
            
            status = visa32.viRead(viSA, readbuf, tmep, out recount);
            Thread.Sleep(4000);
            status = visa32.viPrintf(viSA, "MMEM:DEL \"D:\\rttscr.png\"\n");
            status = visa32.viPrintf(viSA, "*CLS\n");
            //int size = System.BitConverter.ToInt32(readbuf, 1);
            byte[] size = { readbuf[1] };
            //WriteText(Encoding.ASCII.GetString(size));
            byte[] newbuf = readbuf.Skip(2+int.Parse(Encoding.ASCII.GetString(size))).Take(recount - 9).ToArray();
            
            File.WriteAllBytes(_snapPath  + filename, newbuf);
            

        }
        //all command process
        private string command_Process(string cmd,int delay=0,string filename = "")
        {
            

            bool printtag = true;
            string result = "";
            char[] cl = { ':' };
            string sendcmd = "";
            //if(cmd!=String.Empty)
            //{
            Addhistory(cmd);
            LogManager.WriteLog(LogFile.Command, cmd);
            //WriteTraceText(cmd);
            

            //根据前缀判断仪器
            if (cmd.Contains("SA."))
            {
                if (cmd.Contains(Constant.PRIFIX_SA))
                {

                    sendcmd = cmd.Replace(Constant.PRIFIX_SA, "").TrimStart(cl);
                    if (this.addr.SA != "")
                    {
                        try
                        {
                            if(this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd,this.addr.SA, sendcmd, ref this.sesnSA, ref this.viSA, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd,this.addr.SA, sendcmd, this.sa_sesn,delay );
                            }
                            if(this.tag_sa.BackColor == Color.Pink)
                            {
                                this.tag_sa.BackColor = Color.SpringGreen;
                            }
                            
                            
                        }
                        catch (Exception e)
                        {
                            this.tag_sa.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup SA address first!");
                        
                    }



                }
                else if (cmd.Contains("SA.Capture"))
                {
                    WriteTraceText(cmd);
                    if (this.addr.SA != "")
                    {
                        if(this.VisaSwitch)   //only in visa32
                            this.SaCapture(filename);
                        else
                            this.SaCapturebyVisacom(filename);
                    }
                        
                    else
                        WriteTraceText("Please setup SA address first!");
                    

                   
                }
            }
                
            else if (cmd.Contains(Constant.PRIFIX_SG))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_SG, "").TrimStart(cl);
                    if (this.addr.SG != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.SG, sendcmd, ref this.sesnSG, ref this.viSG, delay);
                                
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.SG, sendcmd, this.sg_sesn, delay );
                            }
                        if (this.tag_sg1.BackColor == Color.Pink)
                        {
                            this.tag_sg1.BackColor = Color.SpringGreen;
                        }


                    }
                        catch (Exception e)
                        {
                            this.tag_sg1.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup SG address first!");
                    
                    }
                        
            }
            else if (cmd.Contains(Constant.PRIFIX_SG2))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_SG2, "").TrimStart(cl);
                    if (this.addr.SG2 != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.SG2, sendcmd, ref this.sesnSG2, ref this.viSG2, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.SG2, sendcmd, this.sg2_sesn, delay );
                            }
                        if (this.tag_sg2.BackColor == Color.Pink)
                        {
                            this.tag_sg2.BackColor = Color.SpringGreen;
                        }


                    }
                        catch (Exception e)
                        {
                            this.tag_sg2.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup SG2 address first!");
                    
                    }
                        
            }
            else if (cmd.Contains(Constant.PRIFIX_IS1))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_IS1, "").TrimStart(cl);
                    if (this.addr.IS1 != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.IS1, sendcmd, ref this.sesnIS, ref this.viIS, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.IS1, sendcmd, this.is_sesn, delay );
                            }
                        if (this.tag_is1.BackColor == Color.Pink)
                        {
                            this.tag_is1.BackColor = Color.SpringGreen;
                        }


                    }
                        catch (Exception e)
                        {
                            this.tag_is1.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup IS1 address first!");
                    }
                    
            }
            else if (cmd.Contains(Constant.PRIFIX_IS2))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_IS2, "").TrimStart(cl);
                    if (this.addr.IS2 != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.IS2, sendcmd, ref this.sesnIS2, ref this.viIS2, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.IS2, sendcmd, this.is2_sesn, delay );
                            }
                        if (this.tag_is2.BackColor == Color.Pink)
                        {
                            this.tag_is2.BackColor = Color.SpringGreen;
                        }


                    }
                        catch (Exception e)
                        {
                            this.tag_is2.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup IS2 address first!");
                    }
                    
            }
            else if (cmd.Contains(Constant.PRIFIX_RFBOX))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_RFBOX, "");
                    if (this.addr.RFBOX != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.RFBOX, sendcmd, ref this.sesnRFBOX, ref this.viRFBOX, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.RFBOX, sendcmd,  this.rfbox_sesn, delay);
                            }
                        if (this.tag_rfbox1.BackColor == Color.Pink)
                        {
                            this.tag_rfbox1.BackColor = Color.SpringGreen;
                        }


                    }
                        catch (Exception e)
                        {
                            this.tag_rfbox1.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup RFBOX address first!");
                    }
                    
            }
            else if (cmd.Contains(Constant.PRIFIX_RFBOX2))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_RFBOX2, "");
                    if (this.addr.RFBOX2 != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.RFBOX2, sendcmd, ref this.sesnRFBOX2, ref this.viRFBOX2, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.RFBOX2, sendcmd,  this.rfbox2_sesn, delay);
                            }
                        if (this.tag_rfbox2.BackColor == Color.Pink)
                        {
                            this.tag_rfbox2.BackColor = Color.SpringGreen;
                        }



                    }
                        catch (Exception e)
                        {
                            this.tag_rfbox2.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup RFBOX2 address first!");
                    }
                   
            }
            else if (cmd.Contains(Constant.PRIFIX_DC5767A))
            {
                sendcmd = cmd.Replace(Constant.PRIFIX_DC5767A, "").TrimStart(cl);
                    if (this.addr.DC5767A != "")
                    {
                        try
                        {
                            if (this.VisaSwitch)  //true == visa32
                            {
                                result = this.send_to_instrument(cmd, this.addr.DC5767A, sendcmd, ref this.sesnDC5767A, ref this.viDC5767A, delay);
                            }
                            else
                            {
                                result = this.send_to_instrument(cmd, this.addr.DC5767A, sendcmd,this.dc5767a_sesn, delay);
                            }
                        if (this.tag_DC5767A.BackColor == Color.Pink)
                        {
                            this.tag_DC5767A.BackColor = Color.SpringGreen;
                        }

                    }
                        catch (Exception e)
                        {
                            this.tag_DC5767A.BackColor = Color.Pink;
                        }
                    }
                    else
                    {
                        WriteTraceText("Please setup DC5767A address first!");
                    }
                    
            }
            else if (cmd.Contains(Constant.PRIFIX_RUMASTER))
            {
                WriteTraceText(cmd);
                sendcmd = cmd.Replace(Constant.PRIFIX_RUMASTER, "").TrimStart(cl);
                if (this.tag_rumaster.BackColor != Color.Pink)
                {
                    try
                    {
                        if (sendcmd.Contains("setIQfile"))
                        {
                            string[] cmds = sendcmd.Split('#');
                            if (cmds.Length >= 3)
                            {
                                this.RumasterSetIQfile(cmds[1], cmds[2]);
                            }


                        }
                        else if (sendcmd.Contains("setCPCfile"))
                        {
                            string[] cmds = sendcmd.Split('#');
                            if (cmds.Length >= 3)
                            {
                                this.RumasterSetCPCfile(cmds[1], cmds[2]);
                            }
                        }
                        else if (sendcmd.Contains("startplayback"))
                        {
                            string[] cmds = sendcmd.Split('#');
                            if (cmds.Length >= 3)
                            {

                                if (cmds[2] == "all")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.AXC, Tiger.Ruma.FlowDataType.IQ };
                                    this.RumasterStartPlayBack(cmds[1], flows);
                                }
                                else if (cmds[2] == "axc")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.AXC };
                                    this.RumasterStartPlayBack(cmds[1], flows);
                                }
                                else if (cmds[2] == "iq")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.IQ };
                                    this.RumasterStartPlayBack(cmds[1], flows);
                                }
                                else
                                {
                                    WriteTraceText("Rumaster Commands format error!");
                                }
                            }
                            else
                            {
                                WriteTraceText("Rumaster Commands format error!");
                            }


                        }
                        else if (sendcmd.Contains("rxevmcaptureOnce"))
                        {
                            string[] cmds = sendcmd.Split('#');     //rxevmcapture#cpriport#filename(option)
                            string _tempfilename;
                            if (cmds.Length >= 2)
                            {
                                try
                                {
                                    
                                    // fill in with port, direction , data tyep and mode, ok?ok
                                    this.icdf.SetFlowDataMode(cmds[1], CpriFlowDirection.RX, FlowDataType.IQ, FlowDataMode.RAW);
                                    this.icdf.StartCapture(cmds[1], Tiger.Ruma.FlowDataType.IQ);
                                    Thread.Sleep(20);
                                    this.icdf.StopCapture(cmds[1], Tiger.Ruma.FlowDataType.IQ);
                                    if (cmds.Length == 2)
                                    {
                                        _tempfilename = DateTime.Now.ToString("HH_mm_ss") + ".cul";
                                    }
                                    else
                                    {
                                        _tempfilename = cmds[2] + ".cul";
                                    }
                                    this.icdf.ExportAllCapturedData(cmds[1], @"c:\RTT\rxevm\" + _tempfilename, "", Tiger.Ruma.ExportFormat.Cul, Tiger.Ruma.UmtsType.LTE);
                                    WriteTraceText("rxevm capture : " + _tempfilename + " on cpri " + cmds[1]);
                                    result = _tempfilename;
                                }
                                catch(Exception exp)
                                {
                                    WriteTraceText(exp.Message + "RX_EVM_Capture failed!");
                                }
                                
                            }
                            else
                            {
                                WriteTraceText("Please command,rxevmcaptureOnce#cpriport");
                            }
                        }
                        else if (sendcmd.Contains("rxevmcaptureStart"))
                        {
                            string[] cmds = sendcmd.Split('#');     //rxevmcapture#cpriport#interval
                            if(cmds.Length>=3)
                            {
                                try
                                {
                                    if (int.Parse(cmds[2]) > 2000)
                                    {
                                        _cpriport = cmds[1];
                                        _evmtimer.Interval = int.Parse(cmds[2]);
                                        _evmtimer.Enabled = true;
                                        _evmtimer.Elapsed += new System.Timers.ElapsedEventHandler(_evmtimer_Elapsed);
                                        WriteTraceText("EvmCapture start! Interval is : "+ cmds[2]+" ms ");
                                    }
                                }
                                catch(Exception e)
                                {
                                    WriteTraceText("please check interval value" + e.Message);
                                }
                                
                            }
                            else
                            {
                                WriteTraceText("Please command,rxevmcaptureStart#cpriport#interval(ms)");
                            }
                            
                        }
                        else if (sendcmd.Contains("rxevmcaptureStop"))
                        {
                            string[] cmds = sendcmd.Split('#');     //rxevmcapture#cpriport
                            if (cmds.Length >= 1)
                            {

                                _evmtimer.Stop();
                                WriteTraceText("EvmCapture stop !");
                                

                            }

                        }
                        else if (sendcmd.Contains("stopplayback"))
                        {
                            string[] cmds = sendcmd.Split('#');
                            if (cmds.Length >= 3)
                            {
                                if (cmds[2] == "all")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.AXC, Tiger.Ruma.FlowDataType.IQ };
                                    this.RumasterStopPlayBack(cmds[1], flows);
                                }
                                else if (cmds[2] == "axc")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.AXC };
                                    this.RumasterStopPlayBack(cmds[1], flows);
                                }
                                else if (cmds[2] == "iq")
                                {
                                    Tiger.Ruma.FlowDataType[] flows = { Tiger.Ruma.FlowDataType.IQ };
                                    this.RumasterStopPlayBack(cmds[1], flows);
                                }
                                else
                                {
                                    WriteTraceText("Rumaster Commands format error!");
                                }
                            }
                            else
                            {
                                WriteTraceText("Rumaster Commands format error!");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        this.tag_rumaster.BackColor = Color.Pink;
                        WriteErrorText("Cpriflowdata start/stop failed! "+e.Message);
                    }
                }
                else
                {
                    WriteTraceText("Please start rumaster first!");
                }
                    
            }
            else if (cmd.Contains("Process.Delay"))
            {
                WriteTraceText(cmd);
                sendcmd = cmd.Replace("Process.Delay", "").TrimStart('(').TrimEnd(')');
                try
                {
                    Thread.Sleep(int.Parse(sendcmd));
                }
                catch
                {
                    WriteTraceText("error on : " + cmd);
                }
            }
            else if (cmd.Contains("SERIAL2."))          //serial 2
            {
                sendcmd = cmd.Replace("SERIAL2.", "").TrimStart(cl);
                this.send_to_serial2(cmd);
            }
            else if(cmd.Contains("ts."))
            {
                WriteTraceText(cmd);
                printtag = false;
                
                WriteTraceText(cmd);
                //LogManager.WriteLog(LogFile.Trace, cmd);
                string buttonname = cmd.Replace("ts.", "");
                    
                //get current tabs in mainform
                foreach (NewTabPage ntp in this.tabControl1.TabPages)
                {
                    foreach (Control item in ntp.Controls)
                    {
                        if (item is TableLayoutPanel)
                        {
                            TableLayoutPanel layoutPanel = item as TableLayoutPanel;
                            foreach (Control btn in layoutPanel.Controls)
                            {
                                if (btn is TabButton)
                                {

                                    TabButton button = btn as TabButton;
                                    if (button.Name == buttonname)
                                    {
                                        _tscmd = button._data;
                                        _tsthread = new Thread(new ThreadStart(tsListExecute));
                                        _tsthread.Start();
                                        if (delay != 0)
                                            Thread.Sleep(delay);
                                        else
                                            Thread.Sleep(1000);
                                        //_tsthread.Join();
                                    }

                                }
                            }
                        }

                    }
                    
                        
                }
                    
            }
            //send to rru
            else
            {
                if(this.rruConnButton.CheckState== CheckState.Checked)
                {

                    
                    printtag = false;
                    //this.dataDisplayBox.AppendText(cmd+"123\n");
                    //delay do not equal to 0, need to return result,start timer and return result
                    if (delay != 0)
                    {
                        if (delay < 500)
                            delay = 500;
                        this.socketTag = true;
                        this.socketbuilder.Clear();
                        this.socketprocesstimer.Interval = delay;
                        this.socketprocesstimer.Start();
                        
                        this.send_to_rru(cmd, delay);
                        
                        Thread.Sleep(delay);
                        for (int i=0; i<5;i++)
                        {
                            string tempstr = this.socketbuilder.ToString().TrimEnd();
                            if (tempstr.TrimEnd().EndsWith("$")|| tempstr.Contains('$'))
                            {
                                result = tempstr;
                                WriteDebugText("resp result is: "+ result);
                                break;
                            }
                            else
                            {
                                if(i!=4)
                                {
                                    Thread.Sleep(200);
                                    continue;
                                }
                                else
                                {
                                    tempstr = this.socketbuilder.ToString().TrimEnd();
                                    result = tempstr;
                                    WriteDebugText("socket result from rru: " + result);
                                    if (result.Length == 0)
                                        result = socketNoresult;
                                    break;
                                }
                                
                            }
                                
                        }
                        this.socketTag = false;

                        //this.socketbuilder.Clear();
                    }
                    else
                    {
                        this.send_to_rru(cmd);
                        result = socketNoresult;
                    }
                }
                else
                {
                    printtag = false;
                    WriteTraceText("please connect rru first.");
                    result = socketNorru;
                }

            }
            //print toscreen and log
            //string logmsg = result.Trim();
            if (printtag)
            {
                
                
                if (result!=string.Empty)
                {
                    WriteTraceText(result);
                    
                }
                else
                {
                    result = socketNoresult;

                }    
            }

            return result;
        }
        //rumaster start
        private void RumasterStartPlayBack(string cpriport,Tiger.Ruma.FlowDataType[] flows)
        {
            try
            {
                
                icdf.StartPlayBack(cpriport, flows);
            }
            catch
            {
                WriteTraceText("Rumaster startplayback failed!");
                
            }
            
        }
        //rumaster stop
        private void RumasterStopPlayBack(string cpriport,Tiger.Ruma.FlowDataType[] flows)
        {
            
            try
            {
                
                icdf.StopPlayBack(cpriport, flows);
            }
            catch
            {
                WriteTraceText("Rumaster stopplayback failed!");
                
            }
        }
        //rumaster switch IQ file
        private void RumasterSetIQfile(string cpriport,string filepath)
        {
            WriteTraceText("RumasterSetIQfile : " + filepath);
            
            try
            {
                
                icdf.IQFileClearAll();
                icdf.IQFileAdd(cpriport, filepath);
                icdf.IQFileSetCurrentByName(cpriport, filepath);
            }
            catch(Exception e)
            {
                WriteTraceText("Rumaster set IQ file failed!--"+e.Message);
                
            }
            
        }
        //rumaster switch CPC file
        private void RumasterSetCPCfile(string cpriport, string filepath)
        {
            WriteTraceText("RumasterSetCPCfile : " + filepath);
            
            try
            {
                
                icdf.CpcFilesClearAll(cpriport);
                icdf.CpcFileAdd(cpriport, filepath);
                icdf.CpcSetAxcMode(cpriport, Tiger.Ruma.TxAxcMode.CPC_FILES);
                icdf.CpcFileSetLoopLength(cpriport, filepath, 2);
                icdf.CpcFileSetCurrent(cpriport, filepath);
            }
            catch(Exception e)
            {
                WriteTraceText("Rumaster set CPC file failed!--"+ e.Message);
                
            }
            
        }

        //timer tick
        private void socketprocesstimer_Tick(object sender,EventArgs e)
        {
            //time up to return
            this.socketprocesstimer.Stop();
            this.socketTag = false;
            
        }

        //to serial 2
        private void send_to_serial2(string cmd = "")
        {
            string cmdneedsend = cmd;

            cmd += this._com2trans;


            Thread.Sleep(200);
            //定义一个变量，记录发送了几个字节  
            int n = 0;
            //16进制发送  
            if (checkBoxHexSend.Checked)
            {
                if (this._COM2 != null)
                {
                    if (this._COM2.IsOpen)
                    {
                        //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数 
                        //cmdneedsend += "\r";
                        MatchCollection mc = Regex.Matches(cmdneedsend, @"(?i)[/da-f]{2}");
                        List<byte> buf = new List<byte>();//填充到这个临时列表中  
                                                          //依次添加到列表中  
                        foreach (Match m in mc)
                        {
                            buf.Add(byte.Parse(m.Value));
                        }
                        //转换列表为数组后发送  
                        lock (_lock_COM2)
                        {
                            this._COM2.Write(buf.ToArray(), 0, buf.Count);
                        }
                        //记录发送的字节数  
                        n = buf.Count;
                    }
                }
                else
                {
                    WriteTraceText("Please connect serial 2 first.");
                    
                }

            }
            else//unicode编码直接发送  
            {
                
                if (this._COM2 != null)
                {
                    if (this._COM2.IsOpen)
                    {

            
                        
                        lock (_lock_COM2)
                        {
                            
                            this._COM2.Write(cmdneedsend);
                            WriteDebugText("write to com_2: " + cmdneedsend);
                            
                        }

            

                    }

                }
                    
                else
                {
                    WriteTraceText("Please connect serial 2 first.");
                    
                }

    

    }
    
}
        //to rru 
        private void send_to_rru(string cmd="",int delay = 0)
        {
            Thread.Sleep(200);
            string cmdneedsend;
            if (cmd != "")
                cmdneedsend = cmd;
            else
                cmdneedsend = InputBox.Text;

            cmdneedsend = cmdneedsend.Replace("\r","").Replace("\n","").TrimStart('\r').TrimStart('\n');
            
            
            
            //定义一个变量，记录发送了几个字节  
            int n = 0;
            //16进制发送  
            if (checkBoxHexSend.Checked)
            {
                //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数 

                MatchCollection mc = Regex.Matches(cmdneedsend, @"(?i)[/da-f]{2}");
                List<byte> buf = new List<byte>();//填充到这个临时列表中  
                //依次添加到列表中  
                foreach (Match m in mc)
                {
                    buf.Add(byte.Parse(m.Value));
                }
                //转换列表为数组后发送
                lock(_lock_RRUCOM)
                {
                    this._COM_RRU.Write(buf.ToArray(), 0, buf.Count);
                }
                
                //记录发送的字节数  
                n = buf.Count;
            }
            else//unicode编码直接发送  
            {
                
                if (this._COM_RRU != null)
                {
                    if(this._COM_RRU.IsOpen)
                    {
                        //this.WriteTraceText("\n");
                        //for (int i = 0; 1!=20;i++)
                        //{
                        //    if (!waitingForreceive)
                        //    {
                        //        Thread.Sleep(200);
                        //    }
                        //    else
                        //    {
                        //        break;
                        //    }
                        //}
                        
                        lock (_lock_RRUCOM)
                        {
                            try
                            {
                                this._COM_RRU.Write(cmdneedsend + '\r');
                                
                                WriteDebugText("write to com_rru: " + cmdneedsend);
                                //waitingForreceive = true;
                            }
                            catch(Exception e)
                            {
                                WriteTraceText(cmd);
                                WriteErrorText("write to com_rru failed:" + e.Message);
                            }
                            
                            

                                
                            

                        }
                        
                        


                    }
                    
                }
                    
                else
                {
                    WriteTraceText(cmd);
                    WriteTraceText("Please connect rru first.");
                    
                }
                    
                

            }
            
        }

        //send cmd to instrument by visa32
        private string send_to_instrument(string fullcmd,string address, string cmd,  ref int session,ref int vi,int delay = 0)
        {
            WriteTraceText(fullcmd);
            cmd += '\n';
            int status = -1;
            string strRd = "";
            string instrumentAddress = address;
            //byte[] buf;
            //string cmd = "*IDN?\n";
            //buf = Encoding.ASCII.GetBytes(cmd);
           // int retcount;


            //lock
            //this.WriteTraceText("\n");
            lock (_lock_Instrument)
            {
                for (int i = 0; i < 5; i++)
                {
                    status = visa32.viPrintf(vi, cmd);
                    //status = visa32.viWrite(vi, buf, buf.Length, out retcount);
                    if (status < visa32.VI_SUCCESS)
                    {
                        visa32.viClear(vi);
                        if (i != 4)
                        {
                            Thread.Sleep(200);
                            continue;

                        }
                        else
                        {
                            WriteErrorText("write to instrument byvisa32 error : " + status.ToString());
                            throw new Exception(status.ToString());
                        }
                        

                    }
                    else
                        break;


                }
                
                
                if (delay != 0&&delay>500)
                    Thread.Sleep(delay);
                else
                    Thread.Sleep(500);
                if (cmd.Contains("?"))
                {

                    for (int i = 0; i < 6; i++)
                    {
                        //visa32.viClear(vi);
                        status = visa32.viRead(vi, out strRd, 4069);
                        
                        if (status < visa32.VI_SUCCESS)
                        {
                            visa32.viClear(vi);
                            if (i != 4)
                            {
                                Thread.Sleep(200);
                                continue;
                                
                            }
                            else
                            {
                                WriteErrorText("read from instrument byvisa32 error : " + status.ToString());
                                throw new Exception(status.ToString());
                            }
                            
                        }
                        else
                            break;
                        
                        
                    }
                    

                    

                }
            }

            /*try
            {
                visa32.viGetDefaultRM(out _session);
                status = visa32.viOpen(_session, instrumentAddress, 0, 1000, out _vi);
                if (status == 0)
                {

                    visa32.viClear(vi);
                    status = visa32.viWrite(_vi, buf, buf.Length, out retcount);
                    if (status != 0)
                    {
                        //SetText(status.ToString());

                        throw new Exception(status.ToString());

                    }
                }
                else
                {
                    //SetText(status.ToString());

                    throw new Exception(status.ToString());
                }
            }
            catch
            {
                throw new Exception(status.ToString());
            }
            if (cmd.Contains("?"))
            {
                if (delay != 0)
                    Thread.Sleep(delay);

                byte[] readbuf = new byte[2048];

                status = visa32.viRead(_vi, readbuf, readbuf.Length, out retcount);

                if (retcount != 0)
                {
                    strRd = Encoding.ASCII.GetString(readbuf, 0, retcount - 1);
                    visa32.viClear(_vi);
                }
                else
                {
                    visa32.viClear(_vi);
                    SetText("read instrument error : " + status.ToString());

                    throw new Exception(status.ToString());
                }



            }*/
            ///////////

            strRd = strRd.TrimEnd().TrimEnd('\n');
            return strRd;
        }

        //send to instrument by visacom
        private string send_to_instrument(string fullcmd,string address,string cmd, IMessage ioDmm,int delay = 0)
        {


            WriteTraceText(fullcmd);
            //IVisaSession basesession = null;
            //IMessage talksession = null;
            
            string strRd = "";
            string instrumentAddress = address;
            //lock
            //this.WriteTraceText("\n");
            lock (_lock_Instrument)
            {



                    ioDmm.Clear();
                    ioDmm.WriteString(cmd);
                    

                    if (delay != 0 && delay > 500)
                        Thread.Sleep(delay);
                    else
                        Thread.Sleep(500);
                    if (cmd.Contains("?"))
                    {
                        for (int i = 0; i < 5; i++)
                        {

                            try
                            {
                                
                                strRd = ioDmm.ReadString(4069);
                                break;
                            }
                            catch (Exception exp)
                            {
                                ioDmm.Clear();
                                if (i != 9)
                                {
                                    Thread.Sleep(200);
                                    continue;

                                }
                                else 
                                {
                                    WriteErrorText("write/read to instrumen by visacom error: " + exp.Message);
                                    throw new Exception(exp.Message);
                                }
                                
                            }
                            


                        }
                        
                        


                    }


                
                
            }
            strRd = strRd.TrimEnd('\n');
            return strRd;
        }

        private void ListenUDPClientConnect()
        {
            int recv;
            byte[] data = new byte[1024];
            string recvStr = "";
            string response = "";
            byte[] resbyte;

            
            newsock.Bind(ipep);//Socket与本地的一个终结点相关联
            //Console.WriteLine("Waiting for a client.....");
            
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);//定义要发送的计算机的地址
            EndPoint Remote = (EndPoint)(sender);//
            while (!_shouldStop)
            {
                try
                {
                    // Send back a response.
                    data = new byte[1024];
                    recv = newsock.ReceiveFrom(data, ref Remote);


                    if (recv != 0)
                    {
                        recvStr = Encoding.ASCII.GetString(data, 0, recv);
                        response = this.SocketCommandProcess(recvStr);

                        //response = recvStr.ToUpper();
                        resbyte = Encoding.ASCII.GetBytes(response);
                        newsock.SendTo(resbyte, resbyte.Length, SocketFlags.None, Remote);
                        WriteDebugText("socket response : " + response);
                        //this.Invoke((EventHandler)(delegate
                        //{
                            //LogManager.WriteLog(LogFile.Error, "socket response : "+response);
                        //}));
                     }
                }
                catch
                {
                    break;
                }
                
  
                
                
            }
            
        }
        //socket server thread
        private void Socket_start_Button_Click(object sender, EventArgs e)
        {

            //start socket thread
            if(this.toolStripButton2.Checked==false)
            {

                //_createServer = new Thread(new ThreadStart(ListenClientConnect));
                newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _createServer = new Thread(new ThreadStart(ListenUDPClientConnect));
                _createServer.Start();
                _shouldStop = false;

                //Console.WriteLine("启动监听{0}成功", serverSocket.LocalEndPoint.ToString());
                //通过Clientsoket发送数据  
                WriteTraceText("Script server started.");
                
                this.toolStripButton2.CheckState = CheckState.Checked;
                //Console.ReadLine();
            }
            //stop socket thread
            else
            {
                //this.RequestStopthread();
                //this.RequestStopUDPthread();
                try
                {
                    _createServer.Abort();
                    newsock.Close();
                    this.toolStripButton2.CheckState = CheckState.Unchecked;
                    WriteTraceText("Script server stoped.");
                }
                catch
                {
                    WriteTraceText("Script server failed.");
                }
                
                
                
                
            }
            
        }
        //stop udp server
        private void RequestStopUDPthread()
        {
            _shouldStop = true;
            byte[] data = new byte[1024];
            try
            {
                
                   _shouldStop = true;
                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001);
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                string welcome = "";
                data = Encoding.ASCII.GetBytes(welcome);
                server.SendTo(data, data.Length, SocketFlags.None, ipep);
                


            }
            catch (Exception e)
            {
                
                WriteErrorText("RequestStopUDP error！" + e);
                
                return;
            }

        }
        private void RequestStopthread()
        {
            try
            {
                _shouldStop = true;
                var msg = "";
                //设定服务器IP地址 
                
                TcpClient client = new TcpClient("127.0.0.1", 8001);
                
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                
                NetworkStream stream = client.GetStream();
                
                stream.Write(data, 0, data.Length);
                
                stream.Close();
                client.Close();
                
                
            }
            catch(Exception e)
            {
                //Console.WriteLine("连接服务器失败，请按回车键退出！");
                WriteErrorText("连接服务器失败，请按回车键退出！" + e.Message);
                //this.dataDisplayBox.AppendText("连接服务器失败，请按回车键退出！"+e+"\n");
                return;
            }
            
        }

        //set cmdprogressbar
        public void SetCmdprogress(int value)
        {
            this.Invoke((EventHandler)(delegate
            {

                this.CmdProgressBar.Value = value;
                

            }));
            
        }

        public void WriteText(string text)
        {
            this.Invoke((EventHandler)(delegate
            {

                this.dataDisplayBox.AppendText(text);
                if (this.checkBox_AutoscrollDown.Checked)
                {
                    this.dataDisplayBox.ScrollToCaret();
                }

            }));
        }

        public void WriteTraceNoText(string text)
        {
            this.Invoke((EventHandler)(delegate
            {


                LogManager.WriteLog(LogFile.Trace, text);

            }));
        }

        public void WriteDebugText(string text)
        {
            if(this._debug)
            {
                this.Invoke((EventHandler)(delegate
                {

                    LogManager.WriteLog(LogFile.Debug, text);

                }));
            }
            
        }

        //display
        public void WriteTraceText(string text)
        {
            this.Invoke((EventHandler)(delegate
            {

                this.dataDisplayBox.AppendText(text+'\n');
                if (this.checkBox_AutoscrollDown.Checked)
                {
                    this.dataDisplayBox.ScrollToCaret();
                }
                LogManager.WriteLog(LogFile.Trace, text);

            }));
        }

        public void WriteErrorText(string text)
        {
            this.Invoke((EventHandler)(delegate
            {

                this.dataDisplayBox.AppendText(text + '\n');
                if (this.checkBox_AutoscrollDown.Checked)
                {
                    this.dataDisplayBox.ScrollToCaret();
                }
                LogManager.WriteLog(LogFile.Error, text);

            }));
        }

        public void WriteErrorNoText(string text)
        {
            this.Invoke((EventHandler)(delegate
            {

                
                LogManager.WriteLog(LogFile.Error, text);

            }));
        }

        //write to displaybox in thread
        public void SetText(string text)
        {
            if (dataDisplayBox.InvokeRequired)
            {
                SetTextCallBack st = new SetTextCallBack(SetText);
                this.Invoke(st, new object[] { text });

            }
            else
            {
                dataDisplayBox.AppendText(text + "\n");
                if (this.checkBox_AutoscrollDown.Checked)
                {
                    this.dataDisplayBox.ScrollToCaret();
                }

            }
        }
        //add to historybox in thread
        private void Addhistory(string text)
        {
            if (historyBox.InvokeRequired)
            {
                AddhistoryCallBack st = new AddhistoryCallBack(Addhistory);
                this.Invoke(st, new object[] { text });

            }
            else
            {
                historyBox.Items.Add(text);
                historyBox.SelectedIndex = historyBox.Items.Count - 1;
                
            }
        }
        //button execute process
        private void commandListExecute()
        {
            if(_buttoncmd!=null)
            {
                foreach (string cmd in _buttoncmd)
                {
                    if (cmd != "")
                    {
                        
                        command_Process(cmd);
                        Thread.Sleep(420);
                    }
                }
            }
            _buttoncmd = null;

        }
        //ts execute process
        private void tsListExecute()
        {
            if (_tscmd != null)
            {
                foreach (string cmd in _tscmd)
                {
                    if (cmd != "")
                    {
                        
                        command_Process(cmd);
                        Thread.Sleep(500);
                        
                    }
                }
            }
            _tscmd = null;

        }
        /// <summary>  
        /// 监听客户端连接  
        /// </summary>  
        private void ListenClientConnect()
        {

            listener = new System.Net.Sockets.TcpListener(IPAddress.Parse("127.0.0.1"), 8001);
            listener.Start();
            try
            {
                
                // Buffer for reading data
                Byte[] recvBytes = new Byte[256];
                String recvStr = null;
                String response = null;
                // Enter the listening loop.
                while (!_shouldStop)
                {
                    
                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = listener.AcceptTcpClient();
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;

                    recvStr = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    //SetText("tag");
                    // Loop to receive all the data sent by the client.
                    try
                    {
                        while ((i = stream.Read(recvBytes, 0, recvBytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            recvStr = System.Text.Encoding.ASCII.GetString(recvBytes, 0, i);

                            // Send back a response.

                            if (recvStr.Length != 0)
                            {

                                response = this.SocketCommandProcess(recvStr);

                                //response = recvStr.ToUpper();
                                byte[] bs = Encoding.ASCII.GetBytes(response);
                                stream.Write(bs, 0, bs.Length);  //返回信息给客户端
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        WriteErrorText("socket error: "+e.Message);
                        //LogManager.WriteLog(LogFile.Trace,e.Message);
                        continue;
                    }
                    
                    

                    // Shutdown and end connection
                    client.Close();
                }
            }
            
            catch(Exception e)
            {
                WriteErrorText("socket listen error: " + e.Message);
            }
            finally
            {
                // Stop listening for new clients.
                if(listener!=null)
                    listener.Stop();
            }
            
        }

        

        //Command buttons event handller
        private void command_button_Click(object sender, EventArgs e)
        {
            if(sender is TabButton)
            {
                MouseEventArgs Mouse_e = (MouseEventArgs)e; 
                //left key
                if(Mouse_e.Button == MouseButtons.Left)
                {
                    TabButton button = sender as TabButton;
                    List<string> data = button._data;
                    _buttoncmd = data;
                    _buttoncmdthread = new Thread(new ThreadStart(commandListExecute));
                    _buttoncmdthread.Start();
                    /*foreach (string cmd in data)
                    {
                        if (cmd != "")
                        {
                            //this.sendCommand_to_Device(cmd);
                            //this.button_command_process(cmd);
                            this.command_Process(cmd);
                            Thread.Sleep(300);
                        }
                    }*/
                }
                
                
            }
        }

        //command buttons data edit
        private void command_button_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TabButton button = sender as TabButton;
                ButtonDataForm bdfm = new ButtonDataForm(button.Name,button._data);

                if (bdfm.ShowDialog() == DialogResult.OK)
                {
                    
                    button._data = bdfm._buttondata;
                    button.Name = bdfm._buttonname;
                    button.Text = bdfm._buttonname;
                }
            }  
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //save log
            if(this._log)
                LogManager.WriteLog(LogFile.Log, this.dataDisplayBox.Text);



            //save button ts file as default ts file
            string localpath = Application.StartupPath;
            string filepath = localpath + @"\default.ts";
            //get current tabs in mainform
            List<Tabcontent> tabs = GetCurrentTabs();

            
            

            TsFileHelper tfh = new TsFileHelper();
            tfh.SaveTab(tabs, filepath);

            //close session
            this.closVisa32sesn();
            this.closeVisacomsesn();
            
            
            
            
            //close serial port 
            if(this._COM_RRU!=null)
            {
                Close_serial_port();
                
                this._COM_RRU = null;
            }
            //close serial 2 
            if (this._COM2 != null)
            {
                Close_serial2_port();

                this._COM2 = null;
            }
            
            //save color
            if (this.backcolor!="")
            {
                ConfigHelper ch = new ConfigHelper();
                ch.UpdateConfig("disp_backcolor", this.backcolor);
            }
            if (this.forecolor != "")
            {
                ConfigHelper ch = new ConfigHelper();
                ch.UpdateConfig("disp_forecolor", this.forecolor);
            }
            //save font
            if (this.fontstyle != "")
            {
                ConfigHelper ch = new ConfigHelper();
                ch.UpdateConfig("disp_font", this.fontstyle);
            }

            //close socket
            if(this.toolStripButton2.Checked==true)
            {
                try
                {
                    this._createServer.Abort();
                    newsock.Close();
                }
                catch
                {
                    WriteTraceText("Stop socket server failed.");
                }
                //this.RequestStopthread();
                //this._createServer.Join();
                //this._createServer = null;
            }

        }

        private void closeVisacomsesn()
        {
            if (this.sa_sesn!=null)
            {
                this.sa_sesn.Clear();
                this.sa_sesn.Close();
                this.sa_sesn = null;
                
            }
            if (this.sg_sesn != null)
            {
                this.sg_sesn.Clear();
                this.sg_sesn.Close();
                this.sg_sesn = null;
            }
            if (this.sg2_sesn != null)
            {
                this.sg2_sesn.Clear();
                this.sg2_sesn.Close();
                this.sg2_sesn = null;
            }
            if (this.rfbox_sesn != null)
            {
                this.rfbox_sesn.Clear();
                this.rfbox_sesn.Close();
                this.rfbox_sesn = null;
            }
            if (this.rfbox2_sesn != null)
            {
                this.rfbox2_sesn.Clear();
                this.rfbox2_sesn.Close();
                this.rfbox2_sesn = null;
            }
            if (this.is_sesn != null)
            {
                this.is_sesn.Clear();
                this.is_sesn.Close();
                this.is_sesn = null;
            }
            if (this.is2_sesn != null)
            {
                this.is2_sesn.Clear();
                this.is2_sesn.Close();
                this.is2_sesn = null;
            }
            if (this.dc5767a_sesn != null)
            {
                this.dc5767a_sesn.Clear();
                this.dc5767a_sesn.Close();
                this.dc5767a_sesn = null;
            }
        }

        private void closVisa32sesn()
        {
            if (this.sesnSA != -1)
            {
                visa32.viClear(this.viSA);
                visa32.viClose(this.viSA);
                visa32.viClose(this.sesnSA);
                this.sesnSA = -1;
            }
            if (this.sesnSG != -1)
            {
                visa32.viClear(this.viSG);
                visa32.viClose(this.viSG);
                visa32.viClose(this.sesnSG);
                this.sesnSG = -1;
            }
            if (this.sesnSG2 != -1)
            {
                visa32.viClear(this.viSG2);
                visa32.viClose(this.viSG2);
                visa32.viClose(this.sesnSG2);
                this.sesnSG2 = -1;
            }
            if (this.sesnRFBOX != -1)
            {
                visa32.viClear(this.viRFBOX);
                visa32.viClose(this.viRFBOX);
                visa32.viClose(this.sesnRFBOX);
                this.sesnRFBOX = -1;
            }
            if (this.sesnRFBOX2 != -1)
            {
                visa32.viClear(this.viRFBOX2);
                visa32.viClose(this.viRFBOX2);
                visa32.viClose(this.sesnRFBOX2);
                this.sesnRFBOX2 = -1;
            }
            if (this.sesnIS != -1)
            {
                visa32.viClear(this.viIS);
                visa32.viClose(this.viIS);
                visa32.viClose(this.sesnIS);
                this.sesnIS = -1;
            }
            if (this.sesnIS2 != -1)
            {
                visa32.viClear(this.viIS2);
                visa32.viClose(this.viIS2);
                visa32.viClose(this.sesnIS2);
                this.sesnIS2 = -1;
            }
            if (this.sesnDC5767A != -1)
            {
                visa32.viClear(this.viDC5767A);
                visa32.viClose(this.viDC5767A);
                visa32.viClose(this.sesnDC5767A);
                this.sesnDC5767A = -1;
            }
            
        }

        private List<Tabcontent> GetCurrentTabs()
        {
            List<Tabcontent> tabs = new List<Tabcontent>();

            //get current tabs in mainform
            foreach (NewTabPage ntp in this.tabControl1.TabPages)
            {
                Tabcontent tab = new Tabcontent();
                tab.tabname = ntp.Name;

                foreach (Control item in ntp.Controls)
                {
                    if (item is TableLayoutPanel)
                    {
                        TableLayoutPanel layoutPanel = item as TableLayoutPanel;
                        foreach (Control btitem in layoutPanel.Controls)
                        {
                            if (btitem is TabButton)
                            {
                                TabButton button = btitem as TabButton;
                                Buttontype bt = new Buttontype();
                                bt.btnname = button.Name;
                                bt.btnnumber = button._index;
                                bt.data = button._data;
                                tab.buttons[button._index] = bt;
                            }
                        }
                    }

                }
                tabs.Add(tab);
            }

            return tabs;
        }

        private void button_clearscreen_Click(object sender, EventArgs e)
        {
            this.dataDisplayBox.Clear();
        }

        private void menuSetup_Click(object sender, EventArgs e)
        {
            Address oldaddr = (Address)this.addr.Clone();
            SetupForm sf = new SetupForm(this.addr);
            if(sf.ShowDialog() == DialogResult.OK)
            {
                this.addr.RRU = sf.port_rru;
                this.addr.Baudrate_rru = sf.baudrate_rru;
                this.addr.Baudrate_com2 = sf.baudrate_2;
                this.addr.SERIAL2 = sf.port_2;
                this.addr.SA = sf.localaddr.SA;
                this.addr.SG = sf.localaddr.SG;
                this.addr.SG2 = sf.localaddr.SG2;
                this.addr.RFBOX = sf.localaddr.RFBOX;
                this.addr.RFBOX2 = sf.localaddr.RFBOX2;
                this.addr.IS1 = sf.localaddr.IS1;
                this.addr.IS2 = sf.localaddr.IS2;
                this.addr.DC5767A = sf.localaddr.DC5767A;
                
                
                foreach(Control ctl in this.SerialpropertyBox.Controls)
                {
                    if(ctl.Name == "label_rruport")
                    {
                        ctl.Text = this.addr.RRU;
                    }
                    else if (ctl.Name == "label_rrubaud")
                    {
                        ctl.Text = this.addr.Baudrate_rru;
                    }
                    else if (ctl.Name == "label_serial2port")
                    {
                        ctl.Text = this.addr.SERIAL2;
                    }
                    else if (ctl.Name == "label_serial2baud")
                    {
                        ctl.Text = this.addr.Baudrate_com2;
                    }
                }
                if (this.addr.RRU != "")
                {
                    
                    if (this._COM_RRU.IsOpen == true)
                        {
                            try
                            {
                                Close_serial_port();
                                //设定port,波特率,无检验位，8个数据位，1个停止位
                                this._COM_RRU = new SerialPort(this.addr.RRU, int.Parse(this.addr.Baudrate_rru), Parity.None, 8, StopBits.One);
                                this._COM_RRU.ReadBufferSize = 2048;
                                this._COM_RRU.ReceivedBytesThreshold = 1;
                                this._COM_RRU.NewLine = "\n";
                                this._COM_RRU.RtsEnable = true;
                                this._COM_RRU.DtrEnable = true;


                                this._COM_RRU.Open();
                                this._COM_RRU.DataReceived += new SerialDataReceivedEventHandler(Com_rru_DataReceived);
                                this._COM_RRU.ErrorReceived += new SerialErrorReceivedEventHandler(Com_rru_ErrorReceived);
                            }
                            catch(Exception exp_rru)
                            {
                                WriteErrorText("Serial port rru init error! please check serial infomation first. " + exp_rru.Message);
                            }
                            
                    }
                                       
                }
                if (this.addr.SERIAL2 != "")
                {
                    if (this._COM2.IsOpen == true)
                    {
                        try {
                            Close_serial2_port();
                            //设定port,波特率,无检验位，8个数据位，1个停止位
                            this._COM2 = new SerialPort(this.addr.SERIAL2, int.Parse(this.addr.Baudrate_com2), Parity.None, 8, StopBits.One);
                            this._COM2.ReadBufferSize = 4096;
                            this._COM2.ReceivedBytesThreshold = 1;
                            this._COM2.NewLine = "\r";
                            this._COM2.RtsEnable = true;
                            this._COM2.DtrEnable = true;


                            this._COM2.Open();
                            this._COM2.DataReceived += new SerialDataReceivedEventHandler(Com_2_DataReceived);
                            this._COM2.ErrorReceived += new SerialErrorReceivedEventHandler(Com2_ErrorReceived);
                        }
                        catch (Exception exp_com2) {
                            WriteErrorText("Serial port 2 init error! please check serial infomation first. "+ exp_com2.Message);
                        }
                        
                    }
                    
                }
                
                if(this.VisaSwitch) //true == visa32
                {
                    this.initInstrumentStatusbyVisa32(this.addr, oldaddr);
                }
                else
                {
                    this.initInstrumentStatus(this.addr, oldaddr);
                }
                

                //save address and port to config file

                ConfigHelper ch = new ConfigHelper();
                Dictionary<string, string> adds = this.addr.UpdateAddress();
                ch.UpdateAddr(adds);
                
                sf.Close();
            }
        }

        ////check and open(close) session by visa32
        private void CheckSessionbyVisa32(string addr, Label instrtag, ref int instrsession, ref int vi)
        {
            int status = -1;
            instrtag.Visible = true;

            visa32.viOpenDefaultRM(out instrsession);
            status = visa32.viOpen(instrsession, addr, visa32.VI_NULL, visa32.VI_NULL, out vi);
                        
            if (status >=visa32.VI_SUCCESS)
            {
                instrtag.BackColor = Color.SpringGreen;
                visa32.viSetAttribute(vi, visa32.VI_ATTR_WR_BUF_OPER_MODE, visa32.VI_FLUSH_ON_ACCESS);
                visa32.viSetAttribute(vi,visa32.VI_ATTR_RD_BUF_OPER_MODE,visa32.VI_FLUSH_ON_ACCESS);
                //visa32.viSetAttribute(vi, visa32.VI_ATTR_TMO_VALUE,1000); 
            }
            else
            {
                WriteTraceText("Please re-check the address :"+ addr);
                instrtag.BackColor = Color.Pink;
            }

               
        }
        //check and open(close) session
        private void CheckSession(string addr,Label instrtag, ResourceManager rm, ref IMessage sesn)
        {
            
            
            instrtag.Visible = true;
            try
            {
                if (rm == null)
                {
                    
                    rm = new Ivi.Visa.Interop.ResourceManager();

                    sesn = (IMessage)rm.Open(addr, AccessMode.NO_LOCK, 0);
                    sesn.Timeout = 2000;
                    
                    instrtag.BackColor = Color.SpringGreen;
                    
                    
                }



            }
            catch (System.Runtime.InteropServices.COMException exp)
            {
                instrtag.BackColor = Color.Pink;
                


            }
      
        }

        //init instrument
        private void initInstrumentStatus(Address addr,Address oldaddr,bool init=false)
        {
            if (addr.SA != string.Empty)
            {
                this.tag_sa.Visible = true;
                this.CheckSession(addr.SA,  this.tag_sa, this.sa_rm, ref this.sa_sesn);
                
            }
            else
            {
                this.tag_sa.Visible = false;
            }
            if (addr.SG != string.Empty)
            {
                this.tag_sg1.Visible = true;
                this.CheckSession(addr.SG,  this.tag_sg1, this.sg_rm, ref this.sg_sesn);
            }
            else
            {
                this.tag_sg1.Visible = false;
            }
            if (addr.SG2 != string.Empty)
            {
                this.tag_sg2.Visible = true;
                this.CheckSession(addr.SG2,  this.tag_sg2, this.sg2_rm, ref this.sg2_sesn);
            }
            else
            {
                this.tag_sg2.Visible = false;
            }
            if (addr.RFBOX != string.Empty)
            {
                this.tag_rfbox1.Visible = true;
                this.CheckSession(addr.RFBOX,  this.tag_rfbox1, this.rfbox_rm, ref this.rfbox_sesn);
            }
            else
            {
                this.tag_rfbox1.Visible = false;
            }
            if (addr.RFBOX2 != string.Empty)
            {
                this.tag_rfbox2.Visible = true;
                this.CheckSession(addr.RFBOX2,  this.tag_rfbox2, this.rfbox2_rm, ref this.rfbox2_sesn);
            }
            else
            {
                this.tag_rfbox2.Visible = false;
            }
            if (addr.IS1 != string.Empty)
            {
                this.tag_is1.Visible = true;
                this.CheckSession(addr.IS1,  this.tag_is1, this.is_rm, ref this.is_sesn);
            }
            else
            {
                this.tag_is1.Visible = false;
            }
            if (addr.IS2 != string.Empty)
            {
                this.tag_is2.Visible = true;
                this.CheckSession(addr.IS2,  this.tag_is2, this.is2_rm, ref this.is2_sesn);
            }
            else
            {
                this.tag_is2.Visible = false;
            }
            if (addr.DC5767A != string.Empty)
            {
                this.tag_DC5767A.Visible = true;
                this.CheckSession(addr.DC5767A,  this.tag_DC5767A, this.dc5767a_rm, ref this.dc5767a_sesn);
            }
            else
            {
                this.tag_DC5767A.Visible = false;
            }
            
            
            
            
            
        }
        ////init instrument
        private void initInstrumentStatusbyVisa32(Address addr, Address oldaddr, bool init = false)
        {
            if(addr.SA!=string.Empty)
            {
                this.tag_sa.Visible = true;
                this.CheckSessionbyVisa32(addr.SA, this.tag_sa, ref this.sesnSA, ref this.viSA);
            }
            else
            {
                this.tag_sa.Visible = false;
            }
            if (addr.SG != string.Empty)
            {
                this.tag_sg1.Visible = true;
                this.CheckSessionbyVisa32(addr.SG, this.tag_sg1, ref this.sesnSG, ref this.viSG);
            }
            else
            {
                this.tag_sg1.Visible = false;
            }
            if (addr.SG2 != string.Empty)
            {
                this.tag_sg2.Visible = true;
                this.CheckSessionbyVisa32(addr.SG2, this.tag_sg2, ref this.sesnSG2, ref this.viSG2);
            }
            else
            {
                this.tag_sg2.Visible = false;
            }
            if (addr.RFBOX != string.Empty)
            {
                this.tag_rfbox1.Visible = true;
                this.CheckSessionbyVisa32(addr.RFBOX, this.tag_rfbox1, ref this.sesnRFBOX, ref this.viRFBOX);
            }
            else
            {
                this.tag_rfbox1.Visible = false;
            }
            if (addr.RFBOX2 != string.Empty)
            {
                this.tag_rfbox2.Visible = true;
                this.CheckSessionbyVisa32(addr.RFBOX2, this.tag_rfbox2, ref this.sesnRFBOX2, ref this.viRFBOX2);
            }
            else
            {
                this.tag_rfbox2.Visible = false;
            }
            if (addr.IS1 != string.Empty)
            {
                this.tag_is1.Visible = true;
                this.CheckSessionbyVisa32(addr.IS1, this.tag_is1, ref this.sesnIS, ref this.viIS);
            }
            else
            {
                this.tag_is1.Visible = false;
            }
            if (addr.IS2 != string.Empty)
            {
                this.tag_is2.Visible = true;
                this.CheckSessionbyVisa32(addr.IS2, this.tag_is2, ref this.sesnIS2, ref this.viIS2);
            }
            else
            {
                this.tag_is2.Visible = false;
            }
            if (addr.DC5767A != string.Empty)
            {
                this.tag_DC5767A.Visible = true;
                this.CheckSessionbyVisa32(addr.DC5767A, this.tag_DC5767A, ref this.sesnDC5767A, ref this.viDC5767A);
            }
            else
            {
                this.tag_DC5767A.Visible = false;
            }

            //this.CheckSession(addr.RUMASTER, oldaddr.RUMASTER, this.tag_rumaster, this.rumaster_session, init);
            
            
        }

        private void DC5767A_ON_Click(object sender, EventArgs e)
        {
            string cmd = "DC5767A.OUTPut ON";
            this.command_Process(cmd);
        }

        private void DC5767A_OFF_Click(object sender, EventArgs e)
        {
            string cmd = "DC5767A.OUTPut OFF";
            this.command_Process(cmd);
        }

        private void SACAPTURE_Click(object sender, EventArgs e)
        {
            if(this.VisaSwitch) //true == visa32
            {
                this.SaCapture();
            }
            else
            {
                this.SaCapturebyVisacom();
            }
            
        }

        

        private void dataDisplayBox_TextChanged(object sender, EventArgs e)
        {
            
            
            if (this.dataDisplayBox.Lines.Length > 600)
            {
                if(this._log)
                    LogManager.WriteLog(LogFile.Log, this.dataDisplayBox.Text);
                this.dataDisplayBox.Clear();
            }
            
        }

        private void InputBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //TextBox tb = sender as TextBox;
            
            
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            
            
            if (e.KeyCode == Keys.Up)
            {
                
                if(this.historyBox.Items.Count!=0&&historyBox.SelectedIndex>0)
                {
                    this.historyBox.SelectedIndex--;
                    if (this.historyBox.SelectedItem!=null)
                        this.InputBox.Text = this.historyBox.SelectedItem.ToString();
                    
                }
                
            }
            if(e.KeyCode == Keys.Down)
            {
                
                if (this.historyBox.Items.Count != 0&&historyBox.SelectedIndex<historyBox.Items.Count-1)
                {
                    this.historyBox.SelectedIndex++;
                    if (this.historyBox.SelectedItem != null)
                        this.InputBox.Text = this.historyBox.SelectedItem.ToString();
                    
                }
            }
        }

        //rumaster stasrtup
        private void Rumasterswitchbutton_Click(object sender, EventArgs e)
        {
            
                // Connect the the TigerApplication service using a local or remote IP address
                tas = new ApplicationControl("127.0.0.1");

                // Get a list with all started applications. If no application is running - start a new application
                string[] tsls = tas.GetTslList();
                if (tsls.Length == 0)
                {
                    tas.StartSpecifiedTsl(@"C:\Program Files\Ericsson\TCA\TSL.exe");
                    tsls = tas.GetTslList();
                }

                // Get attached hardware devices
                tsl = new TslControlClient(tsls[0]);
                string[] hwSnrs = tsl.GetHws();
                if (hwSnrs.Length == 0)
                {
                    
                    throw new Exception("No hardwares detected!");
                    

                }
            
            // Retrieve all running tool services. If no tool service is running - start a new tool
                string[] uris = tsl.GetServiceList(ToolType.ID_RUMA);
                string toolUri;
                if (uris.Length == 1)
                {
                    toolUri = uris[0];
                }
                else if (uris.Length > 1)
                {
                    toolUri = uris[uris.Length - 1];
                }
                else
                {
                    toolUri = tsl.StartService(ToolType.ID_RUMA, hwSnrs[0]);
                }

                this.rumaClient = RumaControlClientFactory.Create(toolUri);
            //RUMA instance created, now start tool must be executed and resource needs to be allocated.

            //rumaClient.RuMaUtilities.SetCustomStartupParametersVee(selectedCpriPorts,

            //                                                    trigger1,

            //                                                    trigger2,

            //                                                    trigger3,

            //                                                    trigger4,

            //                                                    rxPortBuffer,

            //                                                    rxIqBandWidth,

            //                                                    txIqBandWidth,

            //                                                    totalRxBufferSize,

            //                                                   totalTxBufferSize,

            //                                                    allocateAuxPort,

            //                                                    allocateDebugPort);


            this.icdf = this.rumaClient.CpriDataFlow;
            this.tag_rumaster.BackColor = Color.SpringGreen;
                
                
                
            
            
        }

        private void SerialpropertyBox_Enter(object sender, EventArgs e)
        {

        }

        //serial2 connect
        private void Serial2_conn_button_Click(object sender, EventArgs e)
        {
            //rru serialport open ,and listen to datareceive
            if (this.Serial2_conn_button.Checked == false)
            {
                
                //初始化串口，如果未设置串口则弹出对话框提示用户先设置串口
                if (this.addr.SERIAL2 == "")
                {
                    WriteTraceText("Please setup serial 2 port first!");
                    
                }
                else
                {
                    //设定port,波特率,无检验位，8个数据位，1个停止位
                    this._COM2 = new SerialPort(this.addr.SERIAL2, int.Parse(this.addr.Baudrate_com2), Parity.None, 8, StopBits.One);
                    this._COM2.ReadBufferSize = 2048;
                    this._COM2.ReceivedBytesThreshold = 1;
                    this._COM2.NewLine = "\r";
                    this._COM2.DtrEnable = true;
                    this._COM2.RtsEnable = true;



                    //open serial port

                    try
                    {

                        this._COM2.Open();
                        this._COM2.DataReceived += Com_2_DataReceived;
                        this._COM2.ErrorReceived += Com2_ErrorReceived;
                        WriteTraceText("Serialport 2 open ,and listen to datareceive.");
                        
                        this.Serial2_conn_button.CheckState = CheckState.Checked;
                    }
                    catch (Exception ex)
                    {
                        this._COM2 = null;
                        //现实异常信息给客户。  
                        
                        WriteErrorText("Serial port 2 open failed: " + ex.Message);
                    }

                }

            }
            //close serial port
            else
            {
                Close_serial2_port();
                this.Serial2_conn_button.CheckState = CheckState.Unchecked;
            }
        }

        private void visaSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugForm df = new DebugForm();
            if (df.ShowDialog() == DialogResult.OK)
            {
                //this.dataDisplayBox.AppendText(df.visastatus+"\n");
                if(df.visastatus=="visacom")
                {
                    this.VisaSwitch = false; //false ==visacom
                    closVisa32sesn();
                    initInstrumentStatus(this.addr,this.addr,true);
                    WriteTraceText("switch to "+df.visastatus);
                }
                else
                {
                    this.VisaSwitch = true;  //true == visa32
                    closeVisacomsesn();
                    initInstrumentStatusbyVisa32(this.addr, this.addr, true);
                    WriteTraceText("switch to " + df.visastatus);
                }
            }
        }

        //open capture RXEVM data form
        private void rXEVMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(this.rumaClient!=null)
            {
                EVMForm ef = new EVMForm(this.rumaClient, this.icdf);
                ef.Show();
            }
            else
            {
                MessageBox.Show("Please start Rumaster(TCA) first!");
            }
            
        }

        private void button_executepy_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();


            ofd.InitialDirectory = @"c:\";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fName = ofd.FileName;
                //execute python script
                var engine = IronPython.Hosting.Python.CreateEngine();
                var scope = engine.CreateScope();
                var sourceCode = engine.CreateScriptSourceFromFile(fName);
                ICollection<string> Paths = engine.GetSearchPaths();
                Paths.Add(@"C:\receive");
                engine.SetSearchPaths(Paths);
                var actual = sourceCode.Compile().Execute<string>(scope);
                



            }
        }

        private void radio_CR2_CheckedChanged(object sender, EventArgs e)
        {
            
            this._com2trans = "\r";
            
        }

        private void radio_LF2_CheckedChanged(object sender, EventArgs e)
        {
            this._com2trans = "\n";
            
        }

        private void radioC_L2_CheckedChanged(object sender, EventArgs e)
        {
            this._com2trans = "\r\n";
            
        }

        private void historyBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            InputBox.Text = historyBox.SelectedItem.ToString();
        }
        //==========================================================================================
        //refresh instruments connect status
        //==========================================================================================
        private void tag_sa_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.SA, this.tag_sa, ref this.sesnSA, ref this.viSA);
            }
            else
            {
                this.CheckSession(this.addr.SA, this.tag_sa, this.sa_rm, ref this.sa_sesn);
            }
        }

        private void tag_sg1_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(addr.SG, this.tag_sg1, ref this.sesnSG, ref this.viSG);
            }
            else
            {
                this.CheckSession(addr.SG, this.tag_sg1, this.sg_rm, ref this.sg_sesn);
            }
        }

        private void tag_sg2_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.SG2, this.tag_sg2, ref this.sesnSG2, ref this.viSG2);
            }
            else
            {
                this.CheckSession(this.addr.SG2, this.tag_sg2, this.sg2_rm, ref this.sg2_sesn);
            }
        }

        private void tag_rfbox1_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.RFBOX, this.tag_rfbox1, ref this.sesnRFBOX, ref this.viRFBOX);
            }
            else
            {
                this.CheckSession(this.addr.RFBOX, this.tag_rfbox1, this.rfbox_rm, ref this.rfbox_sesn);
            }
        }

        private void tag_rfbox2_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.RFBOX2, this.tag_rfbox2, ref this.sesnRFBOX2, ref this.viRFBOX2);
            }
            else
            {
                this.CheckSession(this.addr.RFBOX2, this.tag_rfbox2, this.rfbox2_rm, ref this.rfbox2_sesn);
            }
        }

        private void tag_is1_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.IS1, this.tag_is1, ref this.sesnIS, ref this.viIS);
            }
            else
            {
                this.CheckSession(this.addr.IS1, this.tag_is1, this.is_rm, ref this.is_sesn);
            }
        }

        private void tag_is2_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.IS2, this.tag_is2, ref this.sesnIS2, ref this.viIS2);
            }
            else
            {
                this.CheckSession(this.addr.IS2, this.tag_is2, this.is2_rm, ref this.is2_sesn);
            }
        }

        private void tag_DC5767A_Click(object sender, EventArgs e)
        {
            if (this.VisaSwitch) //true == visa32
            {
                this.CheckSessionbyVisa32(this.addr.DC5767A, this.tag_DC5767A, ref this.sesnDC5767A, ref this.viDC5767A);
            }
            else
            {
                this.CheckSession(this.addr.DC5767A, this.tag_DC5767A, this.dc5767a_rm, ref this.dc5767a_sesn);
            }
        }

        private void checkBox_debug_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox_debug.Checked)
            {
                this._debug = true;
                WriteTraceText("Debug On.");
            }
            else
            {
                this._debug = false;
                WriteTraceText("Debug Off.");
            }
        }

        private void checkBox_Log_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Log.Checked)
            {
                this._log = true;
                WriteTraceText(" Full log On.");
            }
            else
            {
                this._log = false;
                WriteTraceText("Full log Off.");
            }
        }

        //Update to latest version
        private void upgradeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = false;
            Process[] ps = Process.GetProcesses();
            foreach (Process p in ps)
            {
                //MessageBox.Show(p.ProcessName);
                if (p.ProcessName.ToLower() == "rttupdate")
                {
                    running = true;
                    break;
                }
            }
            if(!running)
            {
                Process.Start(Application.StartupPath + @"\RTTUpdate.exe");
            }
            else
            {
                WriteTraceText("update is running!");
            }
            
        }


        //==================================================================================================
        private void comboBox_instrumentprefix_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_instrumentprefix.SelectedItem.ToString() != "")
            {
                string command = this.InputBox.Text;


                switch (comboBox_instrumentprefix.SelectedIndex)
                {

                    //rru
                    case 0:
                        InputBox.Text = "";
                        //this.send_to_rru();

                        break;
                    //sa
                    case 1:
                        InputBox.Text = Constant.PRIFIX_SA + ":";
                        
                        break;
                    //sg1
                    case 2:
                        InputBox.Text = Constant.PRIFIX_SG + ":";
                        
                        break;
                    //sg2
                    case 3:
                        InputBox.Text = Constant.PRIFIX_SG2 + ":";
                        
                        break;
                    //rfbox1
                    case 4:
                        InputBox.Text = Constant.PRIFIX_RFBOX;
                        
                        break;
                    //rfbox2
                    case 5:
                        InputBox.Text = Constant.PRIFIX_RFBOX2;
                        
                        break;
                    //dc5767a
                    case 6:
                        InputBox.Text = Constant.PRIFIX_DC5767A;
                        
                        break;
                    //is1
                    case 7:
                        InputBox.Text = Constant.PRIFIX_IS1+":";
                        
                        break;
                    //is2
                    case 8:
                        InputBox.Text = Constant.PRIFIX_IS2 + ":";
                        
                        break;
                    //rumaster
                    case 10:
                        InputBox.Text = Constant.PRIFIX_RUMASTER;
                        
                        break;
                    //SERIAL2
                    case 9:
                        InputBox.Text = "SERIAL2.";
                        break;

                    default:

                        break;

                }
            }
        }

        private void historyBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.historyBox.Items.Count > 100)
            {
                this.historyBox.Items.Remove(historyBox.Items[0]);
                //this.historyBox.Items.Clear();
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void cmmenu_tab_Opening(object sender, CancelEventArgs e)
        {

        }

        private void Newtab_Click(object sender, EventArgs e)
        {
            NewTabPage Page;
            //tc has no buttons,add a empty page
            
            Page = new NewTabPage();
            Page.Name = "Page" + _tabindex.ToString();
            Page.Text = "tabPage" + _tabindex.ToString();
            Page.TabIndex = _tabindex;

            TableLayoutPanel layoutPanel = new TableLayoutPanel();
            layoutPanel.RowCount = 4;
            layoutPanel.ColumnCount = 8;
            layoutPanel.Dock = DockStyle.Fill;
            Page.Controls.Add(layoutPanel);
            this.tabControl1.Controls.Add(Page);
            //layout button
            //int x = 8, y = 10;
            for (int i = 0; i != 32; i++)
            {

                TabButton tb = new TabButton();
                tb._index = i;
                toolTip1.SetToolTip(tb, tb.Text);
                tb.Dock = DockStyle.Fill;
                for (int k = 0; k < layoutPanel.RowCount; k++)
                {
                    layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    for (int j = 0; j < layoutPanel.ColumnCount; j++)
                    {
                        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                        layoutPanel.Controls.Add(tb);
                    }
                }
                tb.Click += new System.EventHandler(command_button_Click);
                tb.MouseDown += new MouseEventHandler(command_button_MouseDown);
            }
            
        }

        private void ModifyName_Click(object sender, EventArgs e)
        {
            ModifyTabnameform mtf = new ModifyTabnameform();
            if(mtf.ShowDialog() == DialogResult.OK)
            {
                if(mtf.Tabname!=null)
                {
                    this.tabControl1.SelectedTab.Text = mtf.Tabname;
                    this.tabControl1.SelectedTab.Name = mtf.Tabname;
                }
            }
        }

        private void DeleteTab_Click(object sender, EventArgs e)
        {
            this.tabControl1.TabPages.Remove(this.tabControl1.SelectedTab);
        }

        private void backColorSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if(cd.ShowDialog() == DialogResult.OK)
            {
                this.dataDisplayBox.BackColor = cd.Color;
                this.backcolor = ColorTranslator.ToHtml(cd.Color);
            }
        }

        private void foreColorSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.dataDisplayBox.ForeColor = cd.Color;
                this.forecolor = ColorTranslator.ToHtml(cd.Color);
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog cd = new FontDialog();
            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.dataDisplayBox.Font = cd.Font;
                FontConverter fc = new FontConverter();
                this.fontstyle = fc.ConvertToInvariantString(cd.Font);
                //this.forecolor = ColorTranslator.ToHtml(cd.Color);
            }
        }

        //do capture evm data
        void _evmtimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //WriteDebugText("rxevm capture tick start");
            this.EVMRXCapture(_cpriport);
            //WriteDebugText("rxevm capture tick fin");
        }

        

        //rxevm
        private void EVMRXCapture(string capturecpriport)
        {

            
            try
            {
                this.icdf.StartCapture(capturecpriport, Tiger.Ruma.FlowDataType.IQ);
                Thread.Sleep(20);
                this.icdf.StopCapture(capturecpriport, Tiger.Ruma.FlowDataType.IQ);
                
                string filename = DateTime.Now.ToString("HH_mm_ss") + ".cul";
                this.icdf.ExportAllCapturedData(capturecpriport, @"c:\RTT\rxevm\" + filename, "", Tiger.Ruma.ExportFormat.Cul, Tiger.Ruma.UmtsType.LTE);
                WriteTraceText("rxevm capture : " + filename +" on cpri "+ capturecpriport);
            }
            catch (Exception exp)
            {
                WriteTraceText(exp.Message + "RX_EVM_Capture failed!");
                
            }
        }

        private void dataDisplayBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)//用户是否按下了Ctrl键
            {
                if (e.KeyCode == Keys.C)
                {
                    
                    
                    if (this._COM_RRU != null)
                    {
                        if (this._COM_RRU.IsOpen)
                        {

                            this._COM_RRU.Write(((char)(3)).ToString()+'\r');
                            
                        }

                    }
                }
            }
            else if(e.KeyCode == Keys.F5)  //f5 debug print switch
            {
                if(this._debug)
                {
                    this._debug = false;
                    WriteTraceText("Debug Off.");
                }
                else
                {
                    this._debug = true;
                    WriteTraceText("Debug On.");
                }
            }
            
        }

        

        
    }

    public class Address : ICloneable
    {
        public string RRU = "";
        public string Baudrate_rru = "";
        public string SERIAL2 = "";
        public string Baudrate_com2 = "";
        public string DC5767A = "";
        public string SA = "";
        public string SG = "";
        public string SG2 = "";
        public string RFBOX = "";
        public string RFBOX2 = "";
        public string RUMASTER = "";
        public string IS1 = "";
        public string IS2 = "";

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public void SetAddress(Dictionary<string, string> addrdic,Links link)
        {
            //ConfigHelper ch = new ConfigHelper();
            //Dictionary<string, string> addrdic = ch.GetAddr();
            this.DC5767A = addrdic[Constant.DEVICE_NAME_AGILENT5767A];
            this.IS1 = addrdic[Constant.DEVICE_NAME_INTERFERENCE_SIGNAL_1];
            this.IS2 = addrdic[Constant.DEVICE_NAME_INTERFERENCE_SIGNAL_2];
            this.RFBOX = addrdic[Constant.DEVICE_NAME_RFBOX];
            this.RFBOX2 = addrdic[Constant.DEVICE_NAME_RFBOX2];
            this.RUMASTER = addrdic[Constant.DEVICE_NAME_RuMaster];
            this.SA = addrdic[Constant.DEVICE_NAME_SIGNALANALYZER];
            this.SG = addrdic[Constant.DEVICE_NAME_SIGNALGENERATOR];
            this.SG2 = addrdic[Constant.DEVICE_NAME_SIGNALGENERATOR2];
            this.RRU = addrdic["port_rru"];
            this.Baudrate_rru = addrdic["baudrate_rru"];
            //if(this.RRU!=""&&this.Baudrate_rru!="")
            //{
            //    link.set_port1(this.RRU, this.Baudrate_rru, "", "8", "1");
            //}
            
            
            //this.SERIAL2 = addrdic[Constant.DEVICE_NAME_DC_DH1716A];
        }

        public Dictionary<string, string> UpdateAddress()
        {
            Dictionary<string, string> addrdic = new Dictionary<string, string>();
            addrdic[Constant.DEVICE_NAME_AGILENT5767A] = this.DC5767A;
            addrdic[Constant.DEVICE_NAME_INTERFERENCE_SIGNAL_1]=this.IS1;
            addrdic[Constant.DEVICE_NAME_INTERFERENCE_SIGNAL_2]=this.IS2;
            addrdic[Constant.DEVICE_NAME_RFBOX]=this.RFBOX;
            addrdic[Constant.DEVICE_NAME_RFBOX2]=this.RFBOX2;
            addrdic[Constant.DEVICE_NAME_RuMaster]=this.RUMASTER;
            addrdic[Constant.DEVICE_NAME_SIGNALANALYZER]=this.SA;
            addrdic[Constant.DEVICE_NAME_SIGNALGENERATOR]= this.SG;
            addrdic[Constant.DEVICE_NAME_SIGNALGENERATOR2]= this.SG2;
            addrdic["port_rru"] = this.RRU;
            addrdic["baudrate_rru"] = this.Baudrate_rru;
            //addrdic[Constant.DEVICE_NAME_DC_DH1716A]= this.SERIAL2;
            return addrdic;
        }
    }


    //extend button
    public partial class TabButton : Button
    {
        
        //buttontype 
        //public Buttontype bt = new Buttontype();
        public int _index = 0;
        public List<string> _data = new List<string>();
        
    }

    //extend tabpage
    public partial class NewTabPage : TabPage
    {
        //tabcontent
        Tabcontent tc = new Tabcontent();
    }

    public class Links
    {
        public SerialPort com_rru;
        public SerialPort com_2;

        public string port_rru = "";
        public string baudrate_rru = "";
        public string parity_rru = "";
        public string stopbits_rru = "";
        public string databits_rru = "";

        public string port_2 = "";
        public string baudrate_2 = "";
        public string parity_2 = "";
        public string stopbits_2 = "";
        public string databits_2 = "";

        public bool port1_hasset = false;
        public bool port2_hasset = false;

        public void set_port1(string port, string baudrate, string parity, string stopbits, string databits)
        {
            this.port_rru = port;
            this.baudrate_rru = baudrate;
            this.parity_rru = parity;
            this.stopbits_rru = stopbits;
            this.databits_rru = databits;
            this.port1_hasset = true;
        }
        public void init_port1(Dictionary<string,string> portatt)
        {
            
            
                this.port_rru = portatt["port_rru"];
                this.baudrate_rru = portatt["baudrate_rru"];
                this.parity_rru = portatt["parity_rru"];
                this.stopbits_rru = portatt["stopbits_rru"];
                this.databits_rru = portatt["databits_rru"];
                this.port1_hasset = true;
            
            
        }

        public Dictionary<string, string> get_port1()
        {

            Dictionary<string, string> portatt = new Dictionary<string, string>();
            portatt["port_rru"] = this.port_rru;
            portatt["baudrate_rru"] = this.baudrate_rru;
            portatt["parity_rru"] = this.parity_rru;
            portatt["stopbits_rru"] = this.stopbits_rru;
            portatt["databits_rru"] = this.databits_rru;
            
            return portatt;


        }

        public void set_port2(string port, string baudrate, string parity, string stopbits, string databits)
        {
            
            
                this.port_2 = port;
                this.baudrate_2 = baudrate;
                this.parity_2 = parity;
                this.stopbits_2 = stopbits;
                this.databits_2 = databits;
                this.port2_hasset = true;
            
                
        }



    }


}