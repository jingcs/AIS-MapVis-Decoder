using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

namespace AppDetect {
    public partial class Form2 : Form {
        public Form2() {
            InitializeComponent();
        }
        static string sendPath = @"Enter decoded msg file path";
        private void button1_Click(object sender, EventArgs e) {
            string content = "附件为AIS报文的解析信息列表";
            string ToAddress = richTextBox1.Text;
            Regex r = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");//匹配邮箱格式
            if (r.IsMatch(ToAddress)) {
                if (richTextBox2.Text != null) {
                    content = richTextBox2.Text;
                }
                sendmail(ToAddress, content);
            }
            else
                MessageBox.Show("邮箱格式错误！");
        }

        public void sendmail(string ToAddress,string content) {
            //MessageBox.Show("进入");
            string user = "xxxxx@xxxx.com";//sender
            string password = "xxxxxx";
            string host = "smtp.[outlook/gmail].com";//设置邮件的服务器server
            string mailAddress = "xxxxx@xxxx.com"; //替换成你的hotmail账户 receiver
            

            string title = "【截止" + DateTime.Now + "】AIS报文实时解析报告";
            
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

        private void button2_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}
