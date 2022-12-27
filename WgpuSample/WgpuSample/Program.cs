using System;

namespace WgpuSample
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            NativeApi.start();
        }
    }
}
