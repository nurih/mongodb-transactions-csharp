using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    public class ToolShed
    {
        private readonly IMongoClient client;
        private IMongoDatabase db;

        private IMongoCollection<Person> PersonCollection => db.GetCollection<Person>(nameof(Person));
        private IMongoCollection<Tool> ToolCollection => db.GetCollection<Tool>(nameof(Tool));
        private IMongoCollection<LendingLedger> LendLedgerCollection => db.GetCollection<LendingLedger>(nameof(LendingLedger));

        public ToolShed(IMongoDatabase db)
        {
            this.db = db;

            this.client = db.Client;
        }


        public void CheckOut(int toolId, int personId)
        {
            System.Console.WriteLine($"Check Out tool [{toolId}] to person [{personId}]");

            using (var session = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
            {

                session.StartTransaction(new TransactionOptions(
                      readConcern: ReadConcern.Snapshot,
                      writeConcern: WriteConcern.WMajority));

                try
                {
                    var holdTool = ToolCollection.UpdateOne(session,
                        Builders<Tool>.Filter.Eq(t => t.Id, toolId) & Builders<Tool>.Filter.Eq(t => t.HeldBy, null),
                        Builders<Tool>.Update.Set(t => t.HeldBy, personId));

                    if (holdTool.ModifiedCount != 1)
                    {
                        throw new InvalidOperationException($"Failed updating hold on tool {toolId}. It might be held or non-existent");
                    }

                    LendLedgerCollection.InsertOne(session, new LendingLedger
                    {
                        ToolId = toolId,
                        PersonId = personId,
                        CheckOutTime = DateTime.UtcNow
                    });

                    var toolCount = PersonCollection.UpdateOne(
                               session,
                               Builders<Person>.Filter.Eq(p => p.Id, personId),
                               Builders<Person>.Update.Inc(p => p.ToolCount, 1)
                               );

                    if (toolCount.ModifiedCount != 1)
                    {
                        throw new InvalidOperationException($"Failed increasing tool count on person {personId}");
                    }
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
                    var heldTool = ToolCollection.FindOneAndUpdate(session,
                        Builders<Tool>.Filter.Eq(t => t.Id, toolId)
                            & Builders<Tool>.Filter.Ne(t => t.HeldBy, null),
                        Builders<Tool>.Update.Unset(t => t.HeldBy), new FindOneAndUpdateOptions<Tool>() { ReturnDocument = ReturnDocument.Before });


                    if (heldTool == null)
                    {
                        throw new InvalidOperationException($"Failed removing hold on tool {toolId}. It might be held or non-existent");
                    }

                    int personId = heldTool.HeldBy.Value;

                    var ledger = LendLedgerCollection.UpdateOne(
                        session,
                        Builders<LendingLedger>.Filter.Eq(l => l.ToolId, toolId)
                            & Builders<LendingLedger>.Filter.Eq(l => l.PersonId, personId)
                            & Builders<LendingLedger>.Filter.Eq(l => l.CheckInTime, null),
                        Builders<LendingLedger>.Update.Set(l => l.CheckInTime, DateTime.UtcNow)
                        );

                    if (ledger.ModifiedCount != 1)
                    {
                        throw new InvalidOperationException($@"Failed updating ledger for tool {toolId} by person {personId}. 
                        It might already be returned, or otherwise not previously properly lent out.");
                    }

                    var toolCount = PersonCollection.UpdateOne(
                        session,
                        Builders<Person>.Filter.Eq(p => p.Id, personId),
                        Builders<Person>.Update.Inc(p => p.ToolCount, -1)
                        );

                    if (toolCount.ModifiedCount != 1)
                    {
                        throw new InvalidOperationException($"Failed reducing tool count on person {personId}");
                    }

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