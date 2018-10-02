using System;
using System.Linq;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    public class SeedData
    {
        public static IMongoDatabase Create(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase("ToolShed");

            SeedPerson(db);

            SeedTool(db);

            SeedLendingLedger(db);

            return db;
        }

        private static void SeedLendingLedger(IMongoDatabase db)
        {
            var lendingCollection = db.GetCollection<LendingLedger>(nameof(LendingLedger));
            //if(db.ListCollectionNames().ToList().Any(c=> c == nameof(LendingLedger))){
            db.DropCollection(nameof(LendingLedger));
            db.CreateCollection(nameof(LendingLedger));

        }

        private static void SeedTool(IMongoDatabase db)
        {
            db.DropCollection(nameof(Tool));
            var toolCollection = db.GetCollection<Tool>(nameof(Tool));

            toolCollection.ReplaceOne(d => d.Id == 1, new Tool { Id = 1, Name = "hammer" }, new UpdateOptions() { IsUpsert = true });
            toolCollection.ReplaceOne(d => d.Id == 2, new Tool { Id = 2, Name = "saw" }, new UpdateOptions() { IsUpsert = true });
            toolCollection.ReplaceOne(d => d.Id == 3, new Tool { Id = 3, Name = "drill" }, new UpdateOptions() { IsUpsert = true });
        }

        private static void SeedPerson(IMongoDatabase db)
        {
            db.DropCollection(nameof(Person));
            var personCollection = db.GetCollection<Person>(nameof(Person));
            personCollection.ReplaceOne(d => d.Id == 1, new Person { Id = 1, Name = "bob" }, new UpdateOptions() { IsUpsert = true });
            personCollection.ReplaceOne(d => d.Id == 2, new Person { Id = 2, Name = "ogg" }, new UpdateOptions() { IsUpsert = true });
            personCollection.ReplaceOne(d => d.Id == 3, new Person { Id = 3, Name = "kim" }, new UpdateOptions() { IsUpsert = true });
        }
    }
}