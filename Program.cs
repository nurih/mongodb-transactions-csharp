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
            try
            {

                Attempt(() => toolShed.CheckOut(1, 3));

                Attempt(() => toolShed.CheckIn(1));

                Attempt(() => toolShed.CheckOut(2, 3));

                Attempt(() => toolShed.CheckIn(3));


            }
            catch (Exception e)
            {
                Console.WriteLine($"Lending Tool Failed. ${e}");
            }


            Console.WriteLine("Done.");


        }

        static void Attempt(Action a)
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Attempt Failed:  {e.Message}");
            }

        }
    }
}
