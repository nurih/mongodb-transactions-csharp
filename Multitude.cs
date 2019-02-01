using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    public class Multitude
    {
        private readonly IMongoClient client;
        private IMongoDatabase db;


        private static readonly FilterDefinition<Person> TargetPersonFilter = Builders<Person>.Filter.Eq(p => p.Id, 4);

        private IMongoCollection<Person> PersonCollection => db.GetCollection<Person>(nameof(Person));

        public Multitude(IMongoDatabase db)
        {
            this.db = db;

            this.client = db.Client;
        }


        public void JustReading(int marker)
        {
            OutsiderSees();

            using (var session = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
            {

                session.StartTransaction(new TransactionOptions(
                      readConcern: ReadConcern.Snapshot,
                      writeConcern: WriteConcern.WMajority));

                try
                {
                    PersonCollection.UpdateOne(
                        session,
                        TargetPersonFilter,
                        Builders<Person>.Update.Set(p => p.ToolCount, marker));

                    Console.WriteLine($"Session set ToolCound to {marker}");

                    OutsiderSees();

                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Caught exception during transaction, aborting: {exception.Message}.");
                    session.AbortTransaction();
                    throw;
                }

                session.CommitWithRetry();
                OutsiderSees();
            }
        }

        public void WritingMidTransaction(int marker)
        {
            OutsiderSees();

            using (var session = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
            {

                session.StartTransaction(new TransactionOptions(
                      readConcern: ReadConcern.Snapshot,
                      writeConcern: WriteConcern.WMajority));

                try
                {
                    PersonCollection.UpdateOne(
                        session,
                        TargetPersonFilter,
                        Builders<Person>.Update.Set(p => p.ToolCount, marker));

                    Console.WriteLine($"Session set ToolCound to {marker}");

                    OutsiderSees();

                    System.Console.WriteLine("*** Trying to touch an in-flight document");

                    PersonCollection.UpdateOne(
                        TargetPersonFilter,
                        Builders<Person>.Update.Set(p => p.ToolCount, marker + 1));


                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Caught exception during transaction, aborting: {exception.Message}.");
                    session.AbortTransaction();
                    throw;
                }

                session.CommitWithRetry();
                OutsiderSees();
            }
        }


        private void OutsiderSees()
        {
            var oneSees = PersonCollection.Find(TargetPersonFilter).Single();
            System.Console.WriteLine($"One sees {oneSees}");
        }

    }
}