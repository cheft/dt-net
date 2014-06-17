using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DT
{
    [Serializable]
    public class Message
    {
        private string uuid;

        public string Uuid
        {
            get { return uuid; }
            set { uuid = value; }
        }

        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        private string fileFolder;

        public string FileFolder
        {
            get { return fileFolder; }
            set { fileFolder = value; }
        }

        private long length;

        public long Length
        {
            get { return length; }
            set { length = value; }
        }

        private string noticeServletPath = "";

        public string NoticeServletPath
        {
            get { return noticeServletPath; }
            set { noticeServletPath = value; }
        }

        private string msg = "";

        public string Msg
        {
            get { return msg; }
            set { msg = value; }
        }

        private　MemoryStream data;

        public MemoryStream Data
        {
            get { return data; }
            set { data = value; }
        }

        public Message(string filePath,string fileFolder, string noticeServletPath, string msg)
        {
            this.uuid = System.Guid.NewGuid().ToString();
            FileInfo fi = new FileInfo(filePath);
            this.fileName = fi.Name;
            this.fileFolder = fileFolder;
            this.length = fi.Length;
            this.noticeServletPath = noticeServletPath;
            this.msg = msg;   

        }

        public Message(string filePath, string fileFolder, string noticeServletPath, string msg, bool isSer)
        {
            this.uuid = System.Guid.NewGuid().ToString();
            FileInfo fi = new FileInfo(filePath);
            this.fileName = fi.Name;
            this.fileFolder = fileFolder;
            this.length = fi.Length;
            this.noticeServletPath = noticeServletPath;
            this.msg = msg;
            this.data = new MemoryStream();

            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return;
                    }
                    this.data.Write(buffer, 0, read);
                }
            }
        }

        /*
        public static string SerializeMsg(Message message, string messagePath)
        {
            using (FileStream stream = new FileStream(messagePath, FileMode.Create))
            {
                BinaryFormatter b = new BinaryFormatter();
                b.Serialize(stream, message);
                stream.Flush();
                stream.Close();
            }
            return message.uuid;
        }

        public static Message DeserializeMsg(string messagePath)
        {
            Message m = null;
            using (FileStream stream = new FileStream(messagePath, FileMode.Open, FileAccess.Read))
            {
                BinaryFormatter b = new BinaryFormatter();
                m = b.Deserialize(stream) as Message;
                stream.Close();
            }
            return m;
        }

        public static void writeFile(Message message, string filePath) 
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                message.data.WriteTo(fileStream);
                message.data.Flush();
                message.data.Close();
            }
        }
        */
    }
}
