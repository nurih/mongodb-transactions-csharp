using System;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello MongoDB Transaction");

            var db = SeedData.Create("mongodb://localhost/");


            var toolShed = new ToolShed(db);

            Attempt(() => toolShed.CheckOut(1, 3), "Lend un-held tool to a person");

            Attempt(() => toolShed.CheckIn(1), "Return tool held by someone");

            Attempt(() => toolShed.CheckOut(2, 3), "Lend un-held tool to a person");

            Attempt(() => toolShed.CheckOut(2, 3), "Lend same tool to same person");

            Attempt(() => toolShed.CheckOut(2, 1), "Lend same tool to someone else - should fail");

            Attempt(() => toolShed.CheckIn(3), "Return tool not held by anyone - should fail");

            Attempt(() => toolShed.CheckOut(3, 999), "Lend tool to non-existent person");

            Attempt(() => toolShed.CheckOut(999, 1), "Lend non-existent tool to person");

            Console.WriteLine("Done.");
        }


        static void Attempt(Action action, string experiment)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(experiment);
            Console.WriteLine("======================");
            Console.ReadKey();
            try
            {

                action();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\t *** Failed!:\n\t{e.Message}");
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
