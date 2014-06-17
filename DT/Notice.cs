using System;
using System.Text;
using System.IO;
using System.Net;
using System.Configuration;
using log4net;

namespace DT
{
    public class Notice
    {

        private string fileName;

        private string noticeServletPath;

        private string msg;

        private static readonly ILog log = LogManager.GetLogger(typeof(Notice));


        public Notice(string noticeServletPath, string msg, string fileName)
        {
            this.noticeServletPath = noticeServletPath;
            this.msg = msg;
            this.fileName = fileName;
        }

        public void NoticeSystem()
        {
            if ("".Equals(this.noticeServletPath))
            {
                return;
            }

            string postData = "msg=" + this.msg;
            postData += "&fileName=" + this.fileName;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(postData);

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(this.noticeServletPath);
                myRequest.Method = "POST";
                myRequest.Timeout = 1800000;
                myRequest.ContentType = "application/x-www-form-urlencoded";

                myRequest.ContentLength = data.Length;

                Stream newStream = myRequest.GetRequestStream();
                // Send the data.
                newStream.Write(data, 0, data.Length);
                newStream.Close();
                // Get response
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.Default);
                string content = reader.ReadToEnd();
                log.DebugFormat("{0}?{1} 回调系统成功！", this.noticeServletPath, postData);
            }
            catch (Exception e)
            {
                log.ErrorFormat("{0}?{1} 回调系统失败！错误信息: {2}; 堆栈信息: {3}", this.noticeServletPath, postData, e.Message, e.StackTrace);
            }
            
        }
    }
}
