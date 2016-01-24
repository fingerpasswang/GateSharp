using System.Threading;

namespace Gate
{
    class Program
    {
        static void Main(string[] args)
        {
            var gate = new Gate();
            while (true)
            {
                gate.MainLoop();
                Thread.Sleep(100);
            }
        }
    }
}
