using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using log4net;

namespace DT
{
    public class SerializableHelper
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(SerializableHelper));

        public static void Serialize(Message data, string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(fs, data);
                }

            }
            catch (Exception ex)
            {
                log.ErrorFormat("文件 {0} 序列化出错, 异常信息： {1}", file, ex.Message);
            }
        }

        public static Message DeSerialize(string file)
        {

            Message data = null;
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    data = (Message)formatter.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("文件 {0} 反序列化出错, 异常信息： {1}", file, ex.Message);
            }
            return data;

        }

        public static void SerilizeXml(Message data, string path)
        {
            System.IO.FileStream stream = new FileStream(path, FileMode.Create);
            try
            {
                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(typeof(Message));
                serializer.Serialize(stream, data);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("SerilizeAnObject Exception: {0}", ex.Message);
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }
        }

        public static Message DeserilizeXml(string path)
        {
            Message data = null;
            System.IO.FileStream stream = new FileStream(path, FileMode.Open);
            try
            {
                System.Xml.XmlReader reader = new XmlTextReader(stream);
                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(typeof(Message));
                data = (Message)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("DeserilizeAnObject Exception: {0}", ex.Message);
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }
            return data;
        }

        public static void writeFile(Message message, string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                message.Data.WriteTo(fileStream);
            }
        }
    }
}
