using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    public class ToolShed
    {
        private readonly IMongoClient client;
        private IMongoDatabase db;

        public ToolShed(IMongoDatabase db)
        {
            this.db = db;

            this.client = db.Client;
        }


        public void CheckOut(int toolId, int personId)
        {
            System.Console.WriteLine($"Check Out tool [{toolId}] to person [{personId}]");
         
            using (var session = client.StartSession())
            {

                session.StartTransaction(new TransactionOptions(
                      readConcern: ReadConcern.Snapshot,
                      writeConcern: WriteConcern.WMajority));

                try
                {

                    var personCollection = db.GetCollection<Person>(nameof(Person));
                    var toolCollection = db.GetCollection<Tool>(nameof(Tool));
                    var lendLogCollection = db.GetCollection<LendingLedger>(nameof(LendingLedger));


                    var holdTool = toolCollection.UpdateOne(session,
                        Builders<Tool>.Filter.Eq(t => t.Id, toolId) & Builders<Tool>.Filter.Eq(t => t.HeldBy, null),
                        Builders<Tool>.Update.Set(t => t.HeldBy, personId));

                    if (holdTool.ModifiedCount != 1)
                    {
                        throw new InvalidOperationException("Tool already held by somebody");
                    }

                    lendLogCollection.InsertOne(session, new LendingLedger
                    {
                        ToolId = toolId,
                        PersonId = personId,
                        CheckOutTime = DateTime.UtcNow
                    });

                    personCollection.UpdateOne(
                        session,
                        Builders<Person>.Filter.Eq(p => p.Id, personId),
                        Builders<Person>.Update.Inc(p => p.ToolCount, 1)
                        );

                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Caught exception during transaction, aborting: {exception.Message}.");
                    session.AbortTransaction();
                    throw;
                }

                session.CommitWithRetry();

            }
        }

        public void CheckIn(int toolId)
        {
            System.Console.WriteLine($"Check In tool [{toolId}]");
            
            using (var session = client.StartSession())
            {
                session.StartTransaction(new TransactionOptions(
                      readConcern: ReadConcern.Snapshot,
                      writeConcern: WriteConcern.WMajority));

                try
                {

                    var personCollection = db.GetCollection<Person>(nameof(Person));
                    var toolCollection = db.GetCollection<Tool>(nameof(Tool));
                    var lendLogCollection = db.GetCollection<LendingLedger>(nameof(LendingLedger));


                    var heldTool = toolCollection.FindOneAndUpdate(session,
                        Builders<Tool>.Filter.Eq(t => t.Id, toolId)
                            & Builders<Tool>.Filter.Ne(t => t.HeldBy, null),
                        Builders<Tool>.Update.Unset(t => t.HeldBy), new FindOneAndUpdateOptions<Tool>() { ReturnDocument = ReturnDocument.Before });


                    if (heldTool == null)
                    {
                        throw new InvalidOperationException("Tool not currently held");
                    }

                    int personId = heldTool.HeldBy.Value;

                    lendLogCollection.UpdateOne(
                        session,
                        Builders<LendingLedger>.Filter.Eq(l => l.ToolId, toolId)
                            & Builders<LendingLedger>.Filter.Eq(l => l.PersonId, personId)
                            & Builders<LendingLedger>.Filter.Eq(l => l.CheckInTime, null),
                        Builders<LendingLedger>.Update.Set(l => l.CheckInTime, DateTime.UtcNow)
                        );

                    personCollection.UpdateOne(
                        session,
                        Builders<Person>.Filter.Eq(p => p.Id, personId),
                        Builders<Person>.Update.Inc(p => p.ToolCount, -1)
                        );

                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Caught exception during transaction, aborting: {exception.Message}.");
                    session.AbortTransaction();
                    throw;
                }

                session.CommitWithRetry();

            }
        }
    }
}