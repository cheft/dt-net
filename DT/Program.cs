using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using log4net.Config;

namespace DT
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo(@"DT.exe.config"));

            Receiver receiver = new Receiver();
            Thread r = new Thread(new ThreadStart(receiver.Listen));
            r.Start();

            Thread.Sleep(500);

            Sender sender = new Sender();
            Thread s = new Thread(new ThreadStart(sender.Queue));
            s.Start();
        }
    }

}
