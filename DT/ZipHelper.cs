/***************************************************************************
 * 说明：zip 辅助类
 * 作者：陈海峰
 * 日期：2013-04-26
 * 注意事项：
 * 1、需要添加dll引用：ICSharpCode.SharpZipLib 只支持zip
 ****************************************************************************/
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using log4net;

namespace DT
{
    public class ZipHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ZipHelper));

        #region 解压
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool UnZip(string file, string dir)
        {
            try
            {
                if (!File.Exists(file))
                    return false;

                dir = dir.Replace("/", "\\");
                if (!dir.EndsWith("\\"))
                    dir += "\\";

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                ZipInputStream s = new ZipInputStream(File.OpenRead(file));
               
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);

                    if (directoryName != String.Empty)
                    {
                        Directory.CreateDirectory(dir + directoryName);
                    }

                    if (fileName != String.Empty)
                    {
                        FileStream streamWriter = File.Create(dir + theEntry.Name);

                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }

                        streamWriter.Close();
                    }
                }
                s.Close();
                return true;
            }
            catch (Exception e)
            {
                log.ErrorFormat("文件 {0} 解压出错，异常信息: {1}", file, e.Message);
                return false;
            }
        }
        #endregion

        #region 压缩
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="file"></param>
        public static void Zip(string dir, string file)
        {
            if (dir[dir.Length - 1] != System.IO.Path.DirectorySeparatorChar)
                dir += System.IO.Path.DirectorySeparatorChar;

            try
            {
                ZipOutputStream zipStream = new ZipOutputStream(File.Create(file));
                zipStream.SetLevel(6);  // 压缩级别 0-9
                ZipFiles(dir, zipStream, dir);

                zipStream.Finish();
                zipStream.Close();
            }
            catch (Exception ex)
            {
                log.ErrorFormat("目录 {0} 压缩出错，异常信息: {1}", dir, ex.Message);
            }
        }
        #endregion

        #region 递归压缩
        /// <summary>
        /// 递归压缩文件
        /// </summary>
        /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
        /// <param name="zipStream">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名</param>
        /// <param name="staticFile"></param>
        private static void ZipFiles(string sourceFilePath, ZipOutputStream zipStream, string staticFile)
        {
            Crc32 crc = new Crc32();
            string[] filesArray = Directory.GetFileSystemEntries(sourceFilePath);
            foreach (string file in filesArray)
            {
                if (Directory.Exists(file))                     //如果当前是文件夹，递归
                {
                    ZipFiles(file, zipStream, staticFile);
                }
                else                                            //如果是文件，开始压缩
                {
                    FileStream fileStream = File.OpenRead(file);

                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    string tempFile = file.Substring(staticFile.LastIndexOf("\\") + 1);
                    ZipEntry entry = new ZipEntry(tempFile);

                    entry.DateTime = DateTime.Now;
                    entry.Size = fileStream.Length;
                    fileStream.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    zipStream.PutNextEntry(entry);

                    zipStream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        #endregion

      
    }

}
