using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Configuration;
using System.Threading;
using log4net;

namespace DT
{
    public class Receiver
    {
        private int listenPort;

        private string dtPath;

        private string fileFolder;

        private string tempFolder;

        private int packSize;

        private int queueInterval;


        private static readonly ILog log = LogManager.GetLogger(typeof(Receiver));
        
        public Receiver()
        {
            listenPort = int.Parse(ConfigurationManager.AppSettings["ListenPort"]);
            dtPath = ConfigurationManager.AppSettings["DTPath"];
            fileFolder = ConfigurationManager.AppSettings["FileFolder"];
            tempFolder = ConfigurationManager.AppSettings["TempFolder"];
            packSize = int.Parse(ConfigurationManager.AppSettings["PackSize"]);
            queueInterval = int.Parse(ConfigurationManager.AppSettings["queueInterval"]);
        }

        public void Listen()
        {
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Any, listenPort);
                Listener.Start();
            }
            catch (Exception e)
            {
                log.ErrorFormat("请关闭当前窗口并检查端口是否已被其它程序占用!; 错误信息: {0}; 堆栈信息: {1}", e.Message, e.StackTrace);
                return;
            }
            log.DebugFormat("DT 已启动，请不要关闭此窗口！");
            byte[] RecData = new byte[packSize];
            int RecBytes;

            while(true)
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                string SaveFileName = dtPath + "/" + tempFolder + "/" + System.Guid.NewGuid() + ".zip";
                try
                {
                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();
                        log.DebugFormat("已连接到发送方");
                        int totalrecbytes = 0;
                        int count = 0;
                        bool hasFile = false;
                        FileStream Fs = null;
                        while ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0)
                        {
                            count++;
                            if (!hasFile && count > 0)
                            {
                                Fs = new FileStream(SaveFileName, FileMode.OpenOrCreate, FileAccess.Write);
                                hasFile = true;
                            }
                            Fs.Write(RecData, 0, RecBytes);
                            totalrecbytes += RecBytes;
                        }
                        
                        Fs.Flush();
                        Fs.Close();
                        
                        netstream.Close();
                        client.Close();

                        Message message = null;
                        try
                        {
                            string zipPath = SaveFileName.Replace(".zip", "");
                            ZipHelper.UnZip(SaveFileName, zipPath);
                            File.Delete(SaveFileName);
                            message = SerializableHelper.DeSerialize(zipPath + "/file.info");
                            string file = dtPath + "/" + fileFolder + "/" + message.FileFolder + "/" + message.FileName;
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                            FileInfo t = new FileInfo(zipPath + "/" + message.FileName);
                            
                            File.Move(zipPath + "/" + message.FileName, file);
                            
                            FileInfo fi = new FileInfo(file);
                            if (message.Length != fi.Length)
                            {
                                log.DebugFormat("{0} 文件大小不一致", file);
                            }
                            else
                            {
                                log.DebugFormat("{0} 文件接收成功, zip文件: {1}", file, SaveFileName);
                            }
                            Directory.Delete(zipPath, true);
                        }
                        catch (Exception e)
                        {
                            log.ErrorFormat("文件解析失败! {0}; 错误信息: {1}; 堆栈信息: {2}", SaveFileName, e.Message, e.StackTrace);
                        }
                       
                        Notice notice = new Notice(message.NoticeServletPath, message.Msg, message.FileName);
                        Thread s = new Thread(new ThreadStart(notice.NoticeSystem));
                        s.Start();
                    }
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("文件接收失败! {0}; 错误信息: {1}; 堆栈信息: {2}", SaveFileName, ex.Message, ex.StackTrace);
                    Console.WriteLine("[" + DateTime.Now + "] " + SaveFileName + " 文件接收失败!");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
