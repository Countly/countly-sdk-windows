using CountlySDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CountlySample
{
    class Program
    {
        const String serverURL = "https://try.count.ly";//put your server URL here
        const String appKey = null;//put your server APP key here       

        static void Main(string[] args)
        {           
            new Program().Run();
        }

        public void Run()
        {
            System.Console.WriteLine("Hello to the Countly sample console program");

            if(serverURL == null || appKey == null)
            {
                System.Console.WriteLine("");
                System.Console.WriteLine("Problem encountered, you have not set up either the serverURL or the appKey");
                System.Console.ReadKey();
                return;
            }

            Countly.IsLoggingEnabled = true;
            Countly.StartSession(serverURL, appKey, "1.234");

            while (true)
            {
                System.Console.WriteLine("");
                System.Console.WriteLine("Choose your option:");
                System.Console.WriteLine("1) Sample event");
                System.Console.WriteLine("2) Sample caught exception");
                System.Console.WriteLine("3) Exit");

                ConsoleKeyInfo cki = System.Console.ReadKey();
                System.Console.WriteLine("");

                if (cki.Key == ConsoleKey.D1)
                {
                    System.Console.WriteLine("1");
                    Countly.RecordEvent("Some event");
                }
                else if (cki.Key == ConsoleKey.D2)
                {
                    System.Console.WriteLine("2");
                    //Exception ex = new Exception("This is some bad exception 2");
                    //ex.StackTrace = "FDFd";
                    //Countly.RecordException(ex.Message, "a\nb");

                    try
                    {
                        throw new Exception("This is some bad exception 3");
                    }
                    catch (Exception ex)
                    {
                        Countly.RecordException(ex.Message, ex.StackTrace);
                    }
                }
                else if (cki.Key == ConsoleKey.D3)
                {
                    break;
                }
                else
                {
                    System.Console.WriteLine("Wrong input, please try again.");
                }
            };

            Countly.EndSession();
        }
    }
}
