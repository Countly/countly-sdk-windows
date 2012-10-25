using System;
using System.Collections.Generic;
using System.Text;
using OpenUDIDCSharp;

namespace WindowsDesktopConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("UDID:{0}", OpenUDID.value);
            Console.WriteLine("Corp UDID:{0}", OpenUDID.GetCorpUDID("com.wavespread"));
            Console.WriteLine("Press Entry To Continue ");
            Console.ReadLine();
        }
    }
}
