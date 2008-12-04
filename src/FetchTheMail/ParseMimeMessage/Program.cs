using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParseMimeMessage
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("ParseMimeMessage /input <message.eml file> /outputDir <output directory>");
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }
        }
    }
}
