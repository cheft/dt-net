using System;
using System.Net.Sockets;
using System.IO;
using System.Configuration;
using System.Threading;
using log4net;

namespace DT
{
    public class Sender
    {
        private string dtPath;

        private string taskFolder;

        private string successFolder;

        private string failureFolder;

        private string queueFolder;

        private string tempFolder;

        private int queueInterval;

        private int packSize;

        private string taskPath;

        private string noticeServletPath = "";

        private string msg = "";

        private bool isNotSend = true;

        private readonly object padlock = new object();

        private static readonly ILog log = LogManager.GetLogger(typeof(Sender));

        public Sender()
        {
            dtPath = ConfigurationManager.AppSettings["DTPath"];
            taskFolder = ConfigurationManager.AppSettings["TaskFolder"];
            successFolder = ConfigurationManager.AppSettings["SuccessFolder"];
            failureFolder = ConfigurationManager.AppSettings["FailureFolder"];
            queueFolder = ConfigurationManager.AppSettings["QueueFolder"];
            tempFolder = ConfigurationManager.AppSettings["TempFolder"];
            queueInterval = int.Parse(ConfigurationManager.AppSettings["QueueInterval"]);
            packSize = int.Parse(ConfigurationManager.AppSettings["PackSize"]);
            taskPath = dtPath + "/" + taskFolder;
        }

        public void Queue()
        {
            while (true)
            {
                if (isNotSend)
                {
                    ExecuteQueue();
                }
                Thread.Sleep(queueInterval);
            }
        }

        public void CreateTask(string file, string dirName)
        {
            string ip = "";
            int port = 0;
            string filePath = "";
        
           using(StreamReader  sr = File.OpenText(file))
           {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    string[] kv = s.Split('=');
                    if (kv.Length > 1)
                    {
                        if ("ip".Equals(kv[0].Trim()))
                        {
                            ip = kv[1].Trim();
                        }
                        else if ("port".Equals(kv[0].Trim()))
                        {
                            port = int.Parse(kv[1].Trim());
                        }
                        else if ("filePath".Equals(kv[0].Trim()))
                        {
                            filePath = kv[1].Trim();
                        }
                        else if ("msg".Equals(kv[0].Trim()))
                        {
                            msg = kv[1].Trim();
                        }
                        else if ("noticeServletPath".Equals(kv[0].Trim()))
                        {
                            noticeServletPath = kv[1].Trim();
                        }
                    }
                }
            }
            log.DebugFormat("正在准备文件 : {0}", filePath);
            try
            {
                FileInfo fi = new FileInfo(filePath);
                Message message = new Message(filePath, dirName, noticeServletPath, msg);

                string msgPath = dtPath + "/" + tempFolder + "/" + message.Uuid;
                Directory.CreateDirectory(msgPath);
                SerializableHelper.Serialize(message, msgPath + "/file.info");

                File.Copy(filePath, msgPath + "/" + fi.Name);

                ZipHelper.Zip(msgPath, msgPath + ".zip");
                Thread.Sleep(300);
                Directory.Delete(msgPath, true);
                Send(file, dirName, ip, port, msgPath + ".zip", message);
            }
            catch (Exception e)
            {
                log.ErrorFormat("{0} 创建任务出错! 错误信息: {1}; 堆栈信息: {2}", filePath, e.Message, e.StackTrace);
            }
        }
        
        public void ExecuteQueue()
        {
            lock (padlock)
            {
                string[] dirs = Directory.GetDirectories(taskPath);
                if (dirs.Length <= 0)
                {
                    return;
                }
                foreach (string d in dirs)
                {
                    DirectoryInfo di = new DirectoryInfo(d);
                    string dirName = di.Name;
                    string[] files = Directory.GetFiles(d + "/" + queueFolder);
                    foreach (string f in files)
                    {
                        CreateTask(f, dirName);
                    }

                }
            }
        }


        public void Send(string taskFile, string dirName, string ip, int port, string filePath, Message message)
        {
            isNotSend = false;
            FileInfo taskInfo = null;
            TcpClient tcpclient = null;
            string taskName = null;
            try
            {
                taskInfo = new FileInfo(taskFile);
                taskName = taskInfo.Name;
                tcpclient = new TcpClient(); 
                //use IPaddress port as in the server
                tcpclient.Connect(ip, port);
                //Console.WriteLine("Connected to server");
                NetworkStream stm = tcpclient.GetStream();
                if (stm.CanWrite && stm.CanRead)
                {
                    //do write
                    FileInfo fi = new FileInfo(filePath);
                    
                    int loopCount = (int)(fi.Length / packSize);
                    int lastSize = (int)(fi.Length % packSize);
                    long rdby = 0;
                    int i = 0;
                   
                    FileStream fin = fi.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    log.DebugFormat("{0}; 文件开始发送...", message.FileName);
                    byte[] avgBytes = new byte[packSize];
                    byte[] lastBytes = new byte[lastSize];

                    for (int n = 0; n < loopCount; n++)
                    {
                        i = fin.Read(avgBytes, 0, avgBytes.Length);
                        stm.Write(avgBytes, 0, avgBytes.Length);
                        rdby = rdby + i;
                        if (n % 4096 == 0)
                        {
                            int p = (int)(float.Parse(n + "") / float.Parse(loopCount + "") * 100);
                            if (p % 5 == 0)
                            {
                                log.DebugFormat("{0}%.", p);
                            }
                        }
                    }
                    if (lastSize > 0)
                    {
                        i = fin.Read(lastBytes, 0, lastBytes.Length);
                        stm.Write(lastBytes, 0, lastBytes.Length);
                        rdby = rdby + i;
                    }
                    log.DebugFormat("100% 完成!");
                    stm.Flush();
                    stm.Close();
                    fin.Close();
                    string destFile = dtPath + "/" + taskFolder + "/" + dirName + "/" + successFolder + "/" + taskName;
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(taskFile, destFile);
                    File.Delete(filePath);
                    log.DebugFormat("文件发送成功： {0}", filePath);
                    isNotSend = true;
                }
                else if (!stm.CanRead)
                {
                    log.ErrorFormat("文件发送失败： {0} 文件流不可读!", message.FileName);
                    string destFile = dtPath + "/" + taskFolder + "/" + dirName + "/" + failureFolder + "/" + taskName;
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(taskFile, destFile);
                    File.Delete(filePath);
                    isNotSend = true;
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("文件发送失败： {0}; 错误信息: {1}; 堆栈信息: {2}",  message.FileName, e.Message, e.StackTrace);
                string destFile = dtPath + "/" + taskFolder + "/" + dirName + "/" + failureFolder + "/" + taskName;
                if (File.Exists(destFile))
                {
                    File.Delete(destFile);
                }
                File.Move(taskFile, destFile);
                Console.WriteLine(e.StackTrace);
                File.Delete(filePath);
                isNotSend = true;
            }
            finally
            {
                tcpclient.Close();
            }
        }

    }
}
