using System.Diagnostics;
using System.Threading;
using System;
using System.Configuration;

namespace Launch
{
    class Program
    {
        private static void StartDT()
        {
            Process[] ps = Process.GetProcessesByName("DT");
            if (ps.Length <= 0)
            {
                string DTPath = @"DT.exe";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = DTPath;
                process.StartInfo.Arguments = "DT.exe";
                process.Start();

            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("DT守护进程，请不要关闭！");
            string dtAutoRestartTime = ConfigurationManager.AppSettings["DTAutoRestartTime"];
            string[] times = dtAutoRestartTime.Split(',');
            bool flag = true;
            while (true)
            {
                Thread.Sleep(500);
                StartDT();
                Thread.Sleep(3000);
                if (!flag)
                {
                    Thread.Sleep(60000);
                    flag = true;
                }
                for (int i = 0; i < times.Length; i++ )
                {
                    if (flag && (DateTime.Now.Hour + ":" + DateTime.Now.Minute).Equals(times[i].Trim()))
                    {
                        Process[] ps = Process.GetProcessesByName("DT");
                        if (ps.Length > 0)
                        {
                            ps[0].Kill();
                            flag = false;
                        }
                    }
                }
            }
        }
    }
}
