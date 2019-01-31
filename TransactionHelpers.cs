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
                    if (exception.HasErrorLabel("TransientTransactionError"))
                    {
                        Console.WriteLine("TransientTransactionError, retrying transaction.");
                       // It's transient, trying again might succeed
                        continue;
                    }
                    else
                    {
                        // non-transient failure: should not retry.
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
                    // Driver already attempts a retry ONCE if there's a network class error.

                    // In face of "write concern timeout" exception, driver retries once, but you may see "UnknownTransactionCommitResult" if that failed.
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


