using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ROV_Control
{
    public partial class Form1 : Form
    {
        #region
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;//设置该属性为false，防止后面出现异常“提示线程间操作无效: 从不是创建控件的线程访问它。”
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            ThreadStart get_serial_port_threadStart = new ThreadStart(getport);//通过ThreadStart委托告诉子线程执行什么方法　　
            Thread get_serial_port = new Thread(get_serial_port_threadStart);
            get_serial_port.Start();//启动新线程,不断刷新串口列表
        }
        
        void getport()
        {
            while(true)
            {
                //初始化下拉串口名称列表框   
                string[] ports = SerialPort.GetPortNames();
                Array.Sort(ports);
                comboBox_ROV.Items.Clear();
                comboBox_ROV.Items.AddRange(ports);//提取串口列表rov
                //comboBox_ROV.SelectedIndex = comboBox_ROV.Items.Count > 0 ? 0 : -1;
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(ports);//提取串口列表transmitter
                //comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;
                comboBox4.Items.Clear();
                comboBox4.Items.AddRange(ports);//提取串口列表Receiver
                //comboBox4.SelectedIndex = comboBox4.Items.Count > 0 ? 0 : -1;
                Thread.Sleep(1000);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
            if (e.KeyCode == Keys.Up)//前进
            {
                button2_Click(sender, e);
            }
            if (e.KeyCode == Keys.Down)//后退
            {
                button3_Click(sender, e);
            }
            if (e.KeyCode == Keys.Left)//左转
            {
                button8_Click(sender, e);
            }
            if (e.KeyCode == Keys.Right)//右转
            {
                button6_Click(sender, e);
            }
            if (e.KeyCode == Keys.PageUp)//上升
            {
                button7_Click(sender, e);
            }
            if (e.KeyCode == Keys.PageDown)//下降
            {
                button9_Click(sender, e);
            }
            if (e.KeyCode == Keys.N)//左平移
            {
                button4_Click(sender, e);
            }
            if (e.KeyCode == Keys.M)//右平移
            {
                button5_Click(sender, e);
            }
        }
        Thread autodrive_thread;
        Boolean autodrive_flag;
        private void button18_Click(object sender, EventArgs e)//启动自动光源追踪算法
        {
            if(ROV.IsOpen)
            {
                ThreadStart autodrive_threadStart = new ThreadStart(Autodrive);//通过ThreadStart委托告诉子线程执行什么方法　　
                autodrive_thread = new Thread(autodrive_threadStart);
                if (button18.Text == "Autodrive")
                {
                    autodrive_thread.Start();//启动新线程显示已接收的数据量
                    button18.Text = "offAutodrive";
                    autodrive_flag = true;
                }
                else
                {
                    autodrive_thread.Abort();
                    button18.Text = "Autodrive";
                    autodrive_flag = false;
                }
            }
            else
            {
                MessageBox.Show("ROV串口未打开！");
            }
        }

        void Autodrive()//自动光源追踪算法
        {
            bool alignment = false;//对准标志位
            while ((!alignment)&& autodrive_flag)
            {
                M = -2;//右转
                label9.Text = M.ToString();
                ROV_control_order();
                Thread.Sleep(200);
                if (textBox4.Text.Contains("0"))//经历对准时刻
                {
                    M = 0;//停转
                    label9.Text = M.ToString();
                    ROV_control_order();
                    textBox4.Clear();//清除接收框内容，等待3s确定是否对准
                    Thread.Sleep(3000);
                    if (textBox4.Text.Contains("0"))//已对准
                    {
                        alignment = true;
                        button18.Text = "offAutodrive";
                        autodrive_thread.Abort();
                    }
                    else//由于惯性转过了头
                    {
                        while ((!alignment) && autodrive_flag)
                        {
                            M = 2;//左转
                            label9.Text = M.ToString();
                            ROV_control_order();
                            Thread.Sleep(200);
                            if (textBox4.Text.Contains("0"))//经历对准时刻
                            {
                                M = 0;//停转
                                label9.Text = M.ToString();
                                ROV_control_order();
                                textBox4.Clear();//清除接收框内容，等待3s确定是否对准
                                Thread.Sleep(3000);
                                if (textBox4.Text.Contains("0"))//已对准
                                {
                                    alignment = true;
                                    button18.Text = "offAutodrive";
                                    autodrive_thread.Abort();
                                }
                                else break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
        //ROV控制部分**********************************************************************************************************  
        #region
        private void button1_Click(object sender, EventArgs e)//打开串口ROV
        {
            if (!ROV.IsOpen)
            {
                ROV.PortName = comboBox_ROV.Text;//设置串口名
                ROV.BaudRate = 9600;//设置默认波特率
                try
                {
                    ROV.Open();     //打开串口
                    button1.Text = "关闭串口";
                    comboBox_ROV.Enabled = false;//关闭使能
                    ROV.DataReceived += new SerialDataReceivedEventHandler(ROV_DataReceived);//串口接收处理函数
                }
                catch
                {
                    MessageBox.Show("ROV串口打开失败！");
                }
            }
            else
            {
                try
                {
                    ROV.Close();     //关闭串口
                    button1.Text = "打开串口";
                    comboBox_ROV.Enabled = true;//打开使能
                }
                catch
                {
                    MessageBox.Show("ROV串口关闭失败！");
                }
            }
        }

        private void ROV_DataReceived(object sender, SerialDataReceivedEventArgs e)//显示ROV返回的信息
        {
            try
            {
                string str = ROV.ReadExisting();//字符串方式读
                textBox1.AppendText(str);  //添加文本
                textBox1.ScrollToCaret();    //自动显示至最后行
                ROV_control_order();//不断向ROV发指令，避免点击太快响应跟不上
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        int X = 0,Y=0,M=0,Z=0;
        private void button4_Click(object sender, EventArgs e)
        {
            if (Y < 7)
                Y += 1;
            else
                Y = 7;
            label8.Text = Y.ToString();
            ROV_control_order();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (Y > -7)
                Y -= 1;
            else
                Y = -7;
            label8.Text = Y.ToString();
            ROV_control_order();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (M < 7)
                M += 1;
            else
                M = 7;
            label9.Text = M.ToString();
            ROV_control_order();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (M > -7)
                M -= 1;
            else
                M = -7;
            label9.Text = M.ToString();
            ROV_control_order();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (Z < 7)
                Z += 1;
            else
                Z = 7;
            label10.Text = Z.ToString();
            ROV_control_order();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (Z > -7)
                Z -= 1;
            else
                Z = -7;
            label10.Text = Z.ToString();
            ROV_control_order();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (X < 7)
                X += 1;
            else
                X = 7;
            label7.Text = X.ToString();
            ROV_control_order();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (X > -7)
                X -= 1;
            else
                X = -7;
            label7.Text = X.ToString();
            ROV_control_order();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            X = 0; Y = 0; M = 0; Z = 0;
            label7.Text = X.ToString();
            label8.Text = Y.ToString();
            label9.Text = M.ToString();
            label10.Text = Z.ToString();
            ROV_control_order();
        }

        private string order_convert(string direction)
        {
            switch (direction)
            {
                case "-7": return "1";
                case "-6": return "2";
                case "-5": return "3";
                case "-4": return "4";
                case "-3": return "5";
                case "-2": return "6";
                case "-1": return "7";
                case "1": return "9";
                case "2": return "a";
                case "3": return "b";
                case "4": return "c";
                case "5": return "d";
                case "6": return "e";
                case "7": return "f";
                default: return "8";
            }
        }

        private void ROV_control_order()
        {
            textBox1.Focus();//点击按钮后让textBox1获得焦点，避免焦点落在按钮上使键盘失效
            string order = "#" + order_convert(label7.Text) + order_convert(label8.Text) + order_convert(label9.Text) + order_convert(label10.Text) + "$";
            //发送数据
            if (ROV.IsOpen)
            {//如果串口开启
                ROV.Write(order);//写数据
            }
            else
            {
                MessageBox.Show("串口未打开");
            }
        }
        #endregion
        //发送机部分*************************************************************************************************************
        #region
        private void button10_Click(object sender, EventArgs e)//打开串口Transmitter
        {
            if (!Transmitter.IsOpen)
            {
                Transmitter.PortName = comboBox1.Text;//设置串口名
                Transmitter.BaudRate = Convert.ToInt32(comboBox2.Text, 10);
                Transmitter.Encoding = System.Text.Encoding.GetEncoding("GB2312");//此行非常重要 可解决接收中文乱码问题
                try
                {
                    if (comboBox4.Text == comboBox1.Text)
                    {
                        Transmitter = Receiver;
                        same_serial_flag = true;
                        button17.Text = "关闭串口";
                        comboBox4.Enabled = false;//关闭使能
                        comboBox3.Enabled = false;//关闭使能
                    }
                    Transmitter.Open();     //打开串口
                    button10.Text = "关闭串口";
                    comboBox1.Enabled = false;//关闭使能
                    comboBox2.Enabled = false;//关闭使能
                    Transmitter.DataReceived += new SerialDataReceivedEventHandler(Transmitter_DataReceived);//串口接收处理函数
                }
                catch
                {
                    MessageBox.Show("Transmitter串口打开失败！");
                }
            }
            else
            {
                try
                {
                    if (Ack_thread != null)
                    {
                        Ack_thread.Abort();//无视之前的数据，关闭线程以停止重发
                    }
                    Transmitter.Close();     //关闭串口
                    button10.Text = "打开串口";
                    comboBox1.Enabled = true;//打开使能
                    comboBox2.Enabled = true;//打开使能
                    if (same_serial_flag==true)
                    {
                        button17.Text = "打开串口";
                        comboBox4.Enabled = true;//打开使能
                        comboBox3.Enabled = true;//打开使能
                    }
                }
                catch
                {
                    MessageBox.Show("Transmitter串口关闭失败！");
                }
            }
        }
        static bool even = true;//表示待发送的字节是否为偶数个
        ushort check_sum(List<byte> a, int count)  //计算校验和
        {
            int sum = 0;
            byte left = 0;
            if (a.Count % 2 != 0) //奇数个byte数据在最后补0
            {
                even = false;
                a.Add(left);
            }
            ushort[] u16 = new ushort[a.Count / 2];
            int num = 0;
            for (int i = 0; i < (a.Count) / 2; i++)
            {
                u16[i] = (ushort)((Convert.ToInt16(a[num]) << 8) + Convert.ToInt16(a[num + 1]));//byte[]每两个byte构成一个16位
                num += 2;
            }
            for (int index = 0; index < u16.Length; index++)
            {
                sum += u16[index];//16位数据求和
            }
            while (Convert.ToBoolean(sum >> 16))
            {
                sum = (sum >> 16) + (sum & 0xffff);//溢出位补在后面
            }
            ushort summ;
            return summ = (ushort)((~sum) & 0xffff);
        }

        byte[] tcp_byteArray_resend;
        Thread Ack_thread;
        private void button13_Click(object sender, EventArgs e)//发送字符数据
        {
            if (Transmitter.IsOpen)//如果串口开启
            {
                if (Ack_thread !=null)
                {
                    Ack_thread.Abort();//无视之前的数据，关闭线程以停止重发
                }
                if (SendTbox.Text.Trim() != "")//如果框内不为空则
                {
                    if(comboBox5.Text == "TCP" || comboBox5.Text == "UDP")
                    {
                        byte[] byteArray = System.Text.Encoding.Default.GetBytes(SendTbox.Text.Trim());

                        List<byte> byteList = new List<byte>();
                        byte[] tcp_byteArray = byteList.ToArray();
                        byteList.AddRange(byteArray);

                        byte init_checksum = 0;
                        byteList.Insert(0, init_checksum);
                        byteList.Insert(0, init_checksum);

                        ushort check_result = check_sum(byteList, byteList.Count);
                        byte[] checksum = BitConverter.GetBytes(check_result).Reverse().ToArray();
                        if (even)
                        {
                            byteList.RemoveRange(0, 2);
                            for (int i = checksum.Length - 1; i >= 0; i--)
                            {
                                byteList.Insert(0, checksum[i]);
                            }
                        }
                        else
                        {
                            byteList.RemoveRange(0, 2);
                            byteList.RemoveRange(byteList.Count - 1, 1);
                            for (int i = checksum.Length - 1; i >= 0; i--)
                            {
                                byteList.Insert(0, checksum[i]);
                            }
                        }
                        tcp_byteArray = byteList.ToArray();
                        even = true;
                        Transmitter.Write(tcp_byteArray, 0, tcp_byteArray.Length);
                        if (comboBox5.Text == "TCP")
                        {
                            tcp_byteArray_resend = tcp_byteArray; //备份数据
                            ThreadStart Ack_threadStart = new ThreadStart(resend);//通过ThreadStart委托告诉子线程执行什么方法　　
                            Ack_thread = new Thread(Ack_threadStart);
                            Ack_thread.Start();//启动新线程   
                        }
                    }
                    else
                    {
                        Transmitter.Write(SendTbox.Text.Trim()+'\n');//写数据
                    }
                    
                }
                else
                {
                    MessageBox.Show("发送框没有数据");
                }
            }
            else
            {
                MessageBox.Show("串口未打开");
            }
        }

        string ack_Received;
        private void Transmitter_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ack_Received = Transmitter.ReadExisting();
            textBox2.AppendText(ack_Received + '\n');    //添加文本
            textBox2.ScrollToCaret();    //自动显示至最后行
        }

        void resend()//线程方法不断检测是否收到ACk
        {
            Thread.Sleep(200);
            while (!textBox2.Text.Contains("0"))
            {
                Transmitter.Write(tcp_byteArray_resend, 0, tcp_byteArray_resend.Length);//没收到确认，重新发送上一次的数据
                Thread.Sleep(1000);
            }
            textBox2.Text = null;
            Ack_thread.Abort();//收到确认，关闭线程以停止重发
        }

        Thread Circle_send;
        private void button19_Click(object sender, EventArgs e)//循环发送
        {
            if (button19.Text == "循环发送")
            {
                button19.Text = "停止循环";
                ThreadStart Circle_threadStart = new ThreadStart(F_Circle_send);//通过ThreadStart委托告诉子线程执行什么方法　　
                Circle_send = new Thread(Circle_threadStart);
                Circle_send.Start();
            }
            else
            {
                button19.Text = "循环发送";
                Circle_send.Abort();
            }

        }
        void F_Circle_send()
        {
            while (button19.Text == "停止循环")
            {
                Transmitter.Write(SendTbox.Text.Trim() + '\n');//写数据
                Thread.Sleep(Convert.ToInt32(textBox5.Text, 10));
            }
        }
        private void button11_Click(object sender, EventArgs e)//打开图像文件
        {
            OpenFileDialog opnDlg = new OpenFileDialog();
            opnDlg.Filter = "所有图像文件 | *.bmp; *.pcx; *.png; *.jpg; *.gif;" +
                "*.tif; *.ico; *.dxf; *.cgm; *.cdr; *.wmf; *.eps; *.emf|" +
                "位图( *.bmp; *.jpg; *.png;...) | *.bmp; *.pcx; *.png; *.jpg; *.gif; *.tif; *.ico|" +
                "矢量图( *.wmf; *.eps; *.emf;...) | *.dxf; *.cgm; *.cdr; *.wmf; *.eps; *.emf";
            opnDlg.Title = "打开图像文件";
            opnDlg.ShowHelp = true;
            if (opnDlg.ShowDialog() == DialogResult.OK)
            {
                curFileName = opnDlg.FileName;
                textBox3.Text = curFileName;
                try
                {
                    bmp1 = new Bitmap(curFileName);
                    FileStream fileStream = new FileStream(curFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    picFile = new byte[fileStream.Length];
                    fileStream.Read(picFile, 0, picFile.Length);
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.Message);
                }
            }
        }

        string curFileName;//图像文件名
        Bitmap bmp1 = null;
        byte[] picFile = null;

        private void button12_Click(object sender, EventArgs e)//发送图片
        {
            if (Transmitter.IsOpen)
            {
                if (picFile != null)
                {
                    ThreadStart T_threadStart = new ThreadStart(Transmitter_send);//通过ThreadStart委托告诉子线程执行什么方法　　
                    Thread T_thread = new Thread(T_threadStart);
                    T_thread.Start();//启动新线程发送数据，避免界面锁死
                    ThreadStart Tnum_threadStart = new ThreadStart(Transmitter_sendnum);//通过ThreadStart委托告诉子线程执行什么方法　　
                    Thread Tnum_thread = new Thread(Tnum_threadStart);
                    Tnum_thread.Start();//启动新线程显示剩余还需发送的数据量
                }
                else
                {
                    MessageBox.Show("请选择要发送的图像文件");
                }
            }
            else
            {
                MessageBox.Show("请打开串口");
            }
        }

        void Transmitter_send()//发送机线程函数
        {
            try
            {
                Transmitter.Write(picFile, 0, picFile.Length);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        void Transmitter_sendnum()//实时显示剩余还需发送的数据量
        {
            int sendnum = Transmitter.BytesToWrite;//此处估计有两位校验位，不一定准确
            label15.Text = sendnum.ToString();
            while (Transmitter.BytesToWrite != 0)
            {
                Thread.Sleep(1000);
                sendnum -= Transmitter.BaudRate / 11;
                label15.Text = sendnum.ToString();
            }
            label15.Text = "0";
        }
        #endregion
        //接收机部分*************************************************************************************************************
        #region
        bool same_serial_flag = false;
        private void button17_Click(object sender, EventArgs e)//打开串口Receiver
        {
            if (!Receiver.IsOpen)
            {
                Receiver.PortName = comboBox4.Text;//设置串口名
                Receiver.BaudRate = Convert.ToInt32(comboBox3.Text, 10);
                Receiver.Encoding = System.Text.Encoding.GetEncoding("GB2312");//此行非常重要 可解决接收中文乱码问题
                try
                {
                    if (comboBox4.Text == comboBox1.Text)
                    {
                        Receiver = Transmitter;
                        same_serial_flag = true;
                        button10.Text = "关闭串口";
                        comboBox1.Enabled = false;//关闭使能
                        comboBox2.Enabled = false;//关闭使能
                    }
                    Receiver.Open();     //打开串口
                    button17.Text = "关闭串口";
                    comboBox4.Enabled = false;//关闭使能
                    comboBox3.Enabled = false;//关闭使能
                    Receiver.DataReceived += new SerialDataReceivedEventHandler(Receiver_DataReceived);//串口接收处理函数
                }
                catch
                {
                    MessageBox.Show("Receiver串口打开失败！");
                }
            }
            else
            {
                try
                {
                    Receiver.Close();     //关闭串口
                    button17.Text = "打开串口";
                    comboBox4.Enabled = true;//打开使能
                    comboBox3.Enabled = true;//打开使能
                    if (same_serial_flag == true)
                    {
                        button10.Text = "打开串口";
                        comboBox1.Enabled = true;//打开使能
                        comboBox2.Enabled = true;//打开使能
                    }
                }
                catch
                {
                    MessageBox.Show("Receiver串口关闭失败！");
                }
            }
        }

        Image pic;
        bool picture_flag = false;

        private void Receiver_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (comboBox5.Text == "TCP"|| comboBox5.Text == "UDP" || comboBox5.Text == "字符")
                picture_flag = true;
            else
                picture_flag = false;
            try
            {
                if (picture_flag)
                {
                    Thread.Sleep(100);
                    int rec_num = Receiver.BytesToRead;
                    string data; 
                    if(!(comboBox5.Text == "字符"))
                    {
                        byte[] data_row = new byte[rec_num];
                        Receiver.Read(data_row, 0, rec_num);
                        List<byte> byteList = new List<byte>();
                        byteList.AddRange(data_row);
                        byte left = 0;
                        bool odd_flag = true;
                        if (byteList.Count % 2 != 0)
                        {
                            byteList.Add(left);
                            odd_flag = false;
                        }
                        ushort[] re_u16 = new ushort[byteList.Count / 2];
                        int num = 0;
                        int re_sum = 0;
                        for (int i = 0; i < (byteList.Count) / 2; i++)
                        {
                            re_u16[i] = (ushort)((Convert.ToInt16(byteList[num]) << 8) + Convert.ToInt16(byteList[num + 1]));
                            num += 2;
                        }
                        for (int index = 0; index < re_u16.Length; index++)
                        {
                            re_sum += re_u16[index];
                        }
                        while (Convert.ToBoolean(re_sum >> 16))
                        {
                            re_sum = (re_sum >> 16) + (re_sum & 0xffff);
                        }
                        if ((re_sum) == 0xffff)//判断接收校验和
                        {
                            if (odd_flag)
                            {
                                byteList.RemoveRange(0, 2);
                            }
                            else
                            {
                                byteList.RemoveRange(0, 2);
                                byteList.RemoveRange(byteList.Count - 1, 1);
                            }
                            byte[] tcp_byteArray = byteList.ToArray();
                            data = System.Text.Encoding.Default.GetString(tcp_byteArray);
                            textBox4.AppendText(data + '\n');    //添加文本
                        }
                    }
                    else
                    {
                        textBox4.AppendText(Receiver.ReadExisting());    //添加文本
                    }
                    textBox4.ScrollToCaret();    //自动显示至最后行
                    ThreadStart Rnum_threadStart = new ThreadStart(Received_num);//通过ThreadStart委托告诉子线程执行什么方法　　
                    Thread Rnum_thread = new Thread(Rnum_threadStart);
                    Rnum_thread.Start();//启动新线程显示已接收的数据量
                    Receiver.Write("00000000");//返回确认ack
                }
                else
                {
                    int Bytestoread_num = 1, Bytestoread_num_last = 0;
                    while (Bytestoread_num != Bytestoread_num_last)//循环等待直到图片接收完毕
                    {
                        System.Threading.Thread.Sleep(1000);//暂停1秒
                        Bytestoread_num_last = Bytestoread_num;
                        Bytestoread_num = Receiver.BytesToRead;
                        label18.Text = Receiver.BytesToRead.ToString();
                    }
                    //MessageBox.Show("图片接收完毕！"); 
                    int recl = Receiver.BytesToRead;
                    byte[] recFile = new byte[recl];
                    Receiver.Read(recFile, 0, recl);
                    //MessageBox.Show("图片转字节存储完毕！");
                    if (recFile != null)
                    {
                        System.IO.MemoryStream stream = new System.IO.MemoryStream((byte[])recFile);
                        pic = Image.FromStream(stream);
                    }
                    picture_flag = true;
                    comboBox5.Text = "UDP";
                    comboBox5.Enabled = false;//关闭使能
                    MessageBox.Show("图片接收完毕！");
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message + "未接收到图片");
                picture_flag = true;
                comboBox5.Text = "UDP";
                comboBox5.Enabled = false;//关闭使能

                Receiver.DiscardInBuffer();
                Receiver.DiscardOutBuffer();
                textBox4.Text = null;
                label18.Text = "0";
                pic = null;
                picture_flag = true;
                comboBox5.Enabled = true;//使能
            }
        }

        private void button16_Click(object sender, EventArgs e)//查看图片
        {
            try
            {
                Form2 nf = new Form2();
                nf.Show();
                nf.BackgroundImage = pic;
                nf.Width = pic.Width + 30;
                nf.Height = pic.Height + 50;
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        void Received_num()//实时显示已接收的数据量
        {
            while (textBox4.Text.Length != 0)
            {
                Thread.Sleep(1000);
                label18.Text = textBox4.Text.Length.ToString();
            }
        }

        private void button14_Click(object sender, EventArgs e)//清空缓存
        {
            if (Receiver.IsOpen)
            {
                Receiver.DiscardInBuffer();
                Receiver.DiscardOutBuffer();
                textBox4.Text = null;
                textBox2.Text = null;
                label18.Text = "0";
                pic = null;
                picture_flag = true;
                comboBox5.Enabled = true;//使能
                MessageBox.Show("清除缓存成功");
            }
            else
            {
                textBox2.Text = null;
                MessageBox.Show("Receiver串口还未打开");
            }
        }
        #endregion
    }

}