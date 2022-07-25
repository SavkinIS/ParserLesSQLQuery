using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserLes
{
    class Program
    {
        

        static void Main(string[] args)
        {
            bool flag = true;
            Parser parser = new Parser(150000, 10);
            Task.Run(() => parser.ParseStart());
            ConsoleKey key;
            while (flag)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Нажмите Escape для выхода");
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.Escape)
                {
                    flag = false;
                    return;
                }

            }
        }
    }
}
