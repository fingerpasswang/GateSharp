using System.Threading;
using System;

namespace Gate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("wrong args usage:[frontPort backPort]");
                return;
            }

            int frontPort;
            if (!int.TryParse(args[0], out frontPort))
            {
                Console.WriteLine("start failed, frontPort parsed failed");
                return;
            }

            int backPort;
            if (!int.TryParse(args[1], out backPort))
            {
                Console.WriteLine("start failed, backPort parsed failed");
                return;
            }
      
            var gate = new Gate(frontPort, backPort);
            while (true)
            {
                gate.MainLoop();
                Thread.Sleep(100);
            }
        }
    }
}
