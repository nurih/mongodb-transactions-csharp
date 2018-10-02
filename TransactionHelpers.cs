using System;
using MongoDB.Driver;

namespace Nuri.MongoDB.Transactions
{
    public static class TransactionHelpers
    {
        public static void RunTransactionWithRetry(this IMongoClient client, Action<IMongoClient, IClientSessionHandle> operationSequence, IClientSessionHandle session)
        {
            while (true)
            {
                try
                {
                    operationSequence(client, session); // performs transaction
                    break;
                }
                catch (MongoException exception)
                {
                    // if transient error, retry the whole transaction
                    if (exception.HasErrorLabel("TransientTransactionError"))
                    {
                        Console.WriteLine("TransientTransactionError, retrying transaction.");
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static void CommitWithRetry(this IClientSessionHandle session)
        {
            while (true)
            {
                try
                {
                    session.CommitTransaction();
                    Console.WriteLine("Transaction committed.");
                    break;
                }
                catch (MongoException exception)
                {
                    // can retry commit
                    if (exception.HasErrorLabel("UnknownTransactionCommitResult"))
                    {
                        Console.WriteLine("UnknownTransactionCommitResult, retrying commit operation");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Error during commit: {exception.Message}.");
                        throw;
                    }
                }
            }
        }
    }
}


