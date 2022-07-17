using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms;
using System.IO.Ports;
using System.Net;
using System.Net.Mail;


namespace AppDetect {
    public partial class Form1 : Form {
        public class ScanProvider {
            public System.IO.Ports.SerialPort _serialPort = new System.IO.Ports.SerialPort();
            public List<string> dataset = new List<string>();
            public ScanProvider() {

                // 串口名  
                _serialPort.PortName = "COM3";
                // 波特率  
                _serialPort.BaudRate = 38400;
                // 数据位  
                _serialPort.DataBits = 8;
                // 停止位  
                _serialPort.StopBits = StopBits.One;
                // 无奇偶校验位  
                _serialPort.Parity = Parity.None;
                _serialPort.Handshake = Handshake.None;
                _serialPort.RtsEnable = true;


                _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

                _serialPort.Open();
            }
            public String str = "";
            public void _serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e) {

                byte[] m_recvBytes = new byte[_serialPort.BytesToRead]; //定义缓冲区大小 BytesToRead为接收缓冲区中数据字节数  
                _serialPort.Read(m_recvBytes, 0, m_recvBytes.Length); //从串口读取数据  

                string strResult = Encoding.UTF8.GetString(m_recvBytes, 0, m_recvBytes.Length); //对数据进行转换  
                _serialPort.DiscardInBuffer();
                str = DateTime.Now+strResult;

                datasave(path, strResult);
                //  dataset.Add(strResult);
                //写入txt
                //return strResult;

            }
            //!!改动 改到类外了

        }
        static string path = "xxx/xxxx/xxxxx/xxx";////复制后再处理
        static string path2 = "xxx/xxxx/xxxxx/xxx";//tst
        static string sendPath = "xxx/xxxx/xxxxx/xxx";
        public static int cnt = 0;
        public static void datasave(string path, String s) {
            FileStream fs = new FileStream(path, FileMode.Append);
            //获得字节数组
            byte[] data = System.Text.Encoding.Default.GetBytes(s);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }

        public Form1() {
            InitializeComponent();
            ProgressBar.CheckForIllegalCrossThreadCalls = false;
            tm.Interval = 100;
            tm.Tick += new EventHandler(tm_Tick);

        }
        //计时器 事件
        void tm_Tick(object sender, EventArgs e) {
            autoEvent.Set(); //通知阻塞的线程继续执行
        }


        //测试用
        private void button2_Click(object sender, EventArgs e) {
            //CRC("2015-10-28 13:00:53!AIVDM,1,1,,B,16:=hkP0018eSa:AaN;cb`Kh0@QE,0*61");

            //测试出错位置
            /*
            String bin = "010100000000000011111100001110100101110010001011101000011110101110111000000000000000011110000000000000000000";
            MessageBox.Show(bin.Length + "");
            List<object> info = ParseBin(bin);
            foreach (object i in info) {
                richTextBox1.Text += i;
            }
            */
            //label1.Text = DateTime.Now + "";

        }


        public void saveTst(String s, string path) {
            FileStream fs = new FileStream(path, FileMode.Append);
            //获得字节数组
            byte[] data = System.Text.Encoding.Default.GetBytes(s);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }
        private void scrollBar_Scroll(object sender, ScrollEventArgs e) {
            if (e.NewValue >= 5)//若当前值不小于行号，则不执行后面代码
            {
                return;
            }
            dataGridView1.FirstDisplayedScrollingRowIndex = e.NewValue;
        }

        public void DoData() {
            while (true) {
                if (flag == 1 && autoEvent.WaitOne()) {
                    tm.Stop();
                    clearAll(); flag = 0; //MessageBox.Show("clear");
                    break;
                }

                autoEvent.WaitOne();  //阻塞当前线程，等待通知以继续执行
                ScanProvider sp = new ScanProvider();
                Thread.Sleep(2000);
                sp._serialPort.Close();
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string s = sr.ReadLine();

                while ((s = sr.ReadLine()) != null) {
                    if (s.StartsWith("$")) {
                        continue;
                    }
                    listView1.Items.Add(s);
                    string Info = processInfo(s);
                    richTextBox1.Text += Info;
                }
                fs.Close();
                sr.Close();
                FileStream stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                stream.Close();
            }
        }
        public void clearFile(string pathFile) {
            FileStream stream = File.Open(pathFile, FileMode.OpenOrCreate, FileAccess.Write);
            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(0);
            stream.Close();
        }
        public void DoData2() {
            FileStream fs = new FileStream(path2, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string s = sr.ReadLine();

            while ((s = sr.ReadLine()) != null) {
                autoEvent.WaitOne();  //阻塞当前线程，等待通知以继续执行
                //dataGridView1.Rows.Add(new String[] { s });
                listView1.Items.Add(s);
                string Info = processInfo(s);
                //MessageBox.Show(Info);
                richTextBox1.Text += Info;
               
            }
            fs.Close();
            sr.Close();
        }
        private void button1_Click(object sender, EventArgs e) {
            tm.Stop();
            clearAll();

            tm.Start();
            Thread t = new Thread(DoData);
            //data2本地 dodata串口调试
            t.Start();
        }

        //CRC校验位 s为整条未解密报文(暗文!以后，明文$以后)
        public bool CRC(String s) {
            if (s.StartsWith("$"))
                return false;
            String jy = s.Substring(s.Length - 2, 2);
            if (jy.Equals("hh"))
                return false;
            //本地解析改这里
            //byte[] BAscll = Encoding.Default.GetBytes(s.Substring(1, s.Length - 4));
            byte[] BAscll = Encoding.Default.GetBytes(s.Substring(20, s.Length - 23));
            int cs = 0;
            for (int i = 0; i < BAscll.Length; i++) {
                cs = cs ^ BAscll[i];
                //MessageBox.Show("BA    " + BAscll[i]+"        S    "+s[i]+"         cs         "+cs);
            }
            if (cs == Convert.ToInt16(jy, 16)) {
                //MessageBox.Show("成功验证");
                return true;
            }
            else {
                //MessageBox.Show("cs     " + cs);
                return false;
            }
        }

        public void clearAll() {
            overlay.Clear();
            allinfo.Clear();
            richTextBox1.Text = "";
            //dataGridView1.Rows.Clear();
            listView1.Items.Clear();
            richTextBox3.Text = "";
            //MessageBox.Show(dataGridView1.Rows.Count+"");
        }
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        private System.Windows.Forms.Timer tm = new System.Windows.Forms.Timer();

        private void button3_Click(object sender, EventArgs e) {
            //MessageBox.Show("datagrid:" + dataGridView1.Rows.Count + "allinfo" + allinfo.Count());
            tm.Stop();
            datasave(sendPath, richTextBox1.Text);
        }

        private void button4_Click(object sender, EventArgs e) {
            tm.Start();
        }
        public int flag = 0;
        private void button5_Click(object sender, EventArgs e) {
            flag = 1;
            // MessageBox.Show(" " + autoEvent.WaitOne());
            //tm.Stop();
            //   MessageBox.Show();
            //     tm.Start();
            //tm.Stop();
            clearAll();
            clearFile(sendPath);
            //      tm.Stop();
            //MessageBox.Show("dwqfwrg");

        }
        public string ProcessThread(string ans) {

            Task<List<object>> task1 = new Task<List<object>>(() => ParseBin(ans));//分割
            Task<string> task2 = task1.ContinueWith(m => ShowInfo(task1.Result));//显示
            task1.Start();
            //MessageBox.Show(" ");
            Thread.Sleep(100);
            return task2.Result;

        }


        public string processInfo(string s) {
            string[] origin = s.Split(',');
            string Info = "";//对话框中合并信息
            List<object> info = new List<object>();//待处理信息
            try {
                if (origin[0].Contains("AIVDM")) {
                    Info = "【接收信息】" + "\r\n";
                }
                else if (origin[0].Contains("AIVDO")) {
                    Info = "【发出信息】" + "\r\n";
                }
                else if (origin[0].Contains("AITST")) {
                    Info = "【无效消息】" + "\r\n";
                }
                if (CRC(s)) {

                    string repo = ConnBin(origin);
                    Info = Info + s + "\r\n";
                    if (repo != "当前报文信息不完整") {
                        info.Clear();
                        info = ParseBin(repo);
                        Info = Info + ShowInfo(info) + "\r\n";
                    }
                }
            }
            catch {

                Info += "报文不完整！无法解析" + "\r\n";
            }
            allinfo.Add(Info);
            return Info;
        }
        public Hashtable fullRepo = new Hashtable();//多条报文拼接

        //多条报文拼接,返回连接好的二进制码
        public string ConnBin(String[] raw) {
            string checkFlag = raw[6].Substring(2, 2);//16进制校验位
            checkFlag = Convert.ToString(Convert.ToInt32(checkFlag, 16), 2);//16转10，10转2校验位
            string repoTmp = FormBin(raw[5]);//报文转成二进制码
            int repoSum = Convert.ToInt32(raw[1]);//总共报文条数
            int repoNo = Convert.ToInt32(raw[2]);//第几条报文

            if (repoSum == 1 && repoTmp.Length >= 168) {
                return repoTmp;
            }
            else {
                fullRepo.Add(repoNo, repoTmp);
                //Console.WriteLine("count:" + fullRepo.Count + " reposum " + repoSum);
                //当所有信息都存放在里面后，输出
                if (fullRepo.Count.Equals(repoSum)) {
                    String repo = "";
                    for (int i = 1; i < repoSum + 1; i++) {
                        repo += fullRepo[i];
                        fullRepo.Clear();//清除，等待下一次信息
                        return repo;
                    }
                }
            }
            //allinfo.Add("当前报文信息不完整");
            return "当前报文信息不完整";
            //N条报文的前N条，都不输出信息
        }

        //单字节ASCII码
        public string Asc(string chara) {
            ASCIIEncoding encode = new ASCIIEncoding();
            int asc = (int)encode.GetBytes(chara)[0];
            asc += 40;
            if (asc > 128) {
                asc += 32;
            }
            else {
                asc += 40;
            }
            string bin = Convert.ToString(asc, 2);
            return bin.Substring(bin.Length - 6, bin.Length - 2);
        }

        //整条报文转换为二进制码
        public string FormBin(string chara) {
            string ans = "";//存放所有二进制
            for (int i = 0; i < chara.Length; i++) {
                ans += Asc(chara.Substring(i));
            }
            //Console.WriteLine(ans);
            return ans;
            
        }



        //CRC成功后，二进制切割解密为List
        public List<object> ParseBin(string ans) {

            List<object> info = new List<object>();
            //1~6位 Identifier for this message
            info.Add(Convert.ToInt32(ans.Substring(0, 6), 2));
            info.Add(Convert.ToInt32(ans.Substring(7, 1), 2));
            info.Add(Convert.ToInt32(ans.Substring(9, 29), 2));
            //info.Add(Convert.ToInt32(ans.Substring(39, 3), 2));
            if (Convert.ToInt32(ans.Substring(39, 3), 2).Equals(0L)) {
                info.Add("动力航行中");
            }
            else {
                info.Add("未航行/停止状态");
            }
            //43~50  Rate of turn 转向率
            info.Add(Convert.ToInt32(ans.Substring(43, 7), 2));
            //51-60 speed of ground
            info.Add(Convert.ToInt32(ans.Substring(51, 9), 2));
            //info.Add(Convert.ToInt32(ans.Substring(61, 1), 2));
            //61 position accuracy
            if (Convert.ToInt32(ans.Substring(61, 1), 2).Equals(0L)) {
                info.Add("低精度");
            }
            else {
                info.Add("高精度");
            }
            // 62-89 longititude
            info.Add(Math.Round((Convert.ToInt32(ans.Substring(62, 27), 2) + 0.0) / 600000, 3));
            //info.Add((Convert.ToInt32(ans.Substring(62, 27), 2) + 0.0) / 600000);
            
            //90-116 Lati 四舍五入 小数点
            info.Add(Math.Round((Convert.ToInt32(ans.Substring(90, 26), 2) + 0.0) / 600000,3));
            //117-128 Course over ground
            info.Add(Convert.ToInt32(ans.Substring(117, 11), 2));

            //129-137 True Heading
            info.Add(Math.Round((Convert.ToInt32(ans.Substring(129, 8), 2) + 0.0) / 600000,4));

            //138-143 UTC second when report（XXX seconds past the minute）
            info.Add(Convert.ToInt32(ans.Substring(138, 5), 2));
            
            //144-147 Regional Application 0000=no
            //info.Add(Convert.ToInt32(ans.Substring(144, 3), 2));
            if (Convert.ToInt32(ans.Substring(144, 3), 2).Equals(0L)) {
                info.Add("否");
            }
            else {
                info.Add("是");
            }

            //148 Spare空闲不用
            //149 RAIM Flag, 0 = RAIM not in use
            //info.Add(Convert.ToInt32(ans.Substring(149, 1), 2));
            if (Convert.ToInt32(ans.Substring(149, 1), 2).Equals(0L)) {
                info.Add("RAIM 没有被使用");
            }
            else {
                info.Add("RAIM 正在被使用");
            }
            //150-168 communications State
            info.Add(Convert.ToInt32(ans.Substring(150, 18), 2));

            return info;
        }


        //公共图层
        public GMapOverlay overlay = new GMapOverlay("Marker");
        //地图加载
        private void gMap_control_Load(object sender, EventArgs e) {
            gMap_control.MapProvider = GMapProviders.GoogleChinaMap; //google china 地图           
            gMap_control.MinZoom = 9;  //最小比例
            gMap_control.MaxZoom = 22; //最大比例
            gMap_control.Zoom = 4;
            gMap_control.DragButton = System.Windows.Forms.MouseButtons.Left; //左键拖拽地图
            gMap_control.Position = new PointLatLng(30.95, 121.85); //地图中心位置：惠南
            this.gMap_control.Overlays.Add(overlay);
            //   this.gMap_control.MouseClick += gMap_control_MouseClick;
            //  SingleShow("000001000110001010100001111101101100100000000000000001011000001000101110111110000110000000010001110001100100100100111011000110111000100111110010000000001100000010100010");
            //  SingleShow("000001001101011010100000011101010101110000100000000000000000101000101101110001100010010000010001101010000011101011000000110111101111111111110100000000000000000000000000");
        }

        List<string> allinfo = new List<string>();
        //显示在文本框和地图上
        public string ShowInfo(List<object> info) {
            string Info = "";
            string[] title = {
            "报文的ID                    ","转发指示符                  ","用户的ID                    ",
            "航运状态                    ","转向速率                     ","实际速度                     ",
            "定位精度                    ","经度                          E ","纬度                          N ",
            "实际航向                    ","航首向                       ","已经通过                    ",
            "地域申请                    ","RAIM标志                  ","通信状态                    " };
            for (int i = 0; i < info.Count; i++) {
                Info += title[i] + info[i] + "\r\n";
            }

            double ew = (double)info[7];
            double ns = (double)info[8];

            ///////////////////////////////////////////////////////////////////////////////////////   
            //创建图层(overlay)和标签(marker)，将标签加入图层，再将图层加入控件中
            //Bitmap bitmap = Bitmap.FromFile(@"D:\AisProject\icon.png") as Bitmap;
            //GMapMarker gMapMarker = new GMarkerGoogle(new PointLatLng(ns, ew), bitmap);

            GMapMarker gMapMarker = new GMarkerGoogle(new PointLatLng(ns, ew), GMarkerGoogleType.blue_dot);//绘制地图中心点
            gMapMarker.ToolTipText = Info;
            gMapMarker.ToolTip.Fill = new SolidBrush(Color.FromArgb(180, 58, 172, 211));
            gMapMarker.ToolTip.Foreground = Brushes.White;
            gMapMarker.ToolTip.TextPadding = new Size(10, 15);
            gMapMarker.ToolTip.Format.Alignment = StringAlignment.Near;
            gMapMarker.ToolTip.Font = new Font("Times New Roman", 8);
            // gMapMarker.
            /*
            GMapOverlay o = new GMapOverlay("M");
            o.Markers.Add(gMapMarker);  //向图层中添加标签
            this.gMap_control.Overlays.Add(o);
            */
            overlay.Markers.Add(gMapMarker);  //向图层中添加标签

            return Info;
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            richTextBox3.Text = "";
            int index = dataGridView1.CurrentRow.Index;

            richTextBox3.Text = allinfo[index];

        }

        private void Form1_Load(object sender, EventArgs e) {
            //label1.Text = DateTime.Now + "";
            listView1.Columns[0].Width = 400;
            /*
            ColumnHeader ch = new ColumnHeader();
            ch.Text = "列标题1";   //设置列标题 
            ch.TextAlign = HorizontalAlignment.Left;   //设置列的对齐方式 
            */
            //this.listView1.Columns.Add("报文");   //将列头添加到ListView控件。

        }

        private void listView1_Click(object sender, EventArgs e) {

        }
        public string Num;//设置一个全局变量


        private void listView1_SelectedIndexChanged(object sender, EventArgs e) {
            richTextBox3.Text = "";

            //int index = listView1.Items[listView1.SelectedIndices[0]].Index;
            //richTextBox3.Text = allinfo[index];
            /*
            int length = listView1.SelectedItems.Count;
            for (int i = 0; i < length; i++) {
                string j = (listView1.SelectedItems[i].Index + 1).ToString();
                Num = j;  //给全局变量赋值
                //MessageBox.Show(j);
            }
            */
            //  richTextBox3.Text = allinfo[Convert.ToInt32(Num)];
            ///  int index = this.listView1.SelectedIndices[0];
            //  this.listView1.SelectedIndices.Clear();
            //  ListView lvi = sender as ListView;
            //  string str = lvi.FocusedItem.Tag.ToString();
            //   MessageBox.Show(this.listView1.SelectedItems.Count+"");
            if (this.listView1.SelectedItems.Count > 0) {
                int index = this.listView1.SelectedItems[0].Index;
                richTextBox3.Text = allinfo[index];
            }
        }


        public void sendmail() {
            //MessageBox.Show("进入");
            string user = "xxxxxx@163.com";
            string password = "password";
            string host = "smtp.163.com";//设置邮件的服务器
            string mailAddress = "xxxxxx@163.com"; //替换成你的hotmail账户
            string ToAddress = "xxxxx@163.com";//目标邮件地址。

            string title = "【截止" + DateTime.Now + "】AIS报文实时解析报告";
            string content = "附件为AIS报文的解析信息列表";
            string fileAddress = sendPath;

            //初始化SMTP类
            SmtpClient smtp = new SmtpClient(host);
            smtp.EnableSsl = true; //开启安全连接。
            smtp.Credentials = new NetworkCredential(user, password); //创建用户凭证
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network; //使用网络传送
            MailMessage message = new MailMessage(mailAddress, ToAddress, title, content); //创建邮件
            Attachment attachment = new Attachment(fileAddress);
            message.Attachments.Add(attachment);
            //message.Attachments.Add(new Attachment(fileAddress));
            MessageBox.Show("等待发送中...");
            message.IsBodyHtml = true;  //这里很重要，漏了的话发出去的是html代码
            smtp.Send(message); //发送邮件
            smtp.Dispose();
            message.Dispose();
            attachment.Dispose();

            MessageBox.Show("邮件发送成功！");
            

        }

        private void button6_Click_1(object sender, EventArgs e) {
            Form f = new Form2();
            f.Show();
            //sendmail();
        }


    }
}
