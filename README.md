# MongoDB 4.0 Transactions Demo

Demonstration of MongoDB Transactions implemented in C#

## The Conceptual Scenario

The demo code simulates a reservation pattern scenario.
The use cases are:

- A `Person` checks out a `Tool` which gets tracked in a `LendingLedger`.
- A `Person` checks out a `Tool` and the `LendingLedger` entry is updated to "close" the lending period.
- A `Person` has a `ToolCount` field which must reflect the number of tools checked out to a person.
- A `Tool` has a `HeldBy` field which must point to the person who has that `Tool` checked out, or `null` if not held by anyone.
- The `LendingLedger` tracks currently and past held tools. It logs the `Tool` id, the `Person` id, the `CheckOutTime` and the `CheckInTime`.
- A `Tool` may only be lent out once, and is only available if not currently lent out.

Since the modeling has 3 separate entities tracking the lending operation, a transaction wraps the 2 major cases: checking in and checking out of a tool.

## Running the Example

Transactions in MongoDB require a Replica Set. Make sure your Mongo server is running in Replica Set mode.

If using Docker, you can achieve this by running

```bash
docker run --name mongo-local-v40 -d -p 27017:27017 mongo:4.0 --replSet r1 
```

The above command will create and start a single node MongoDB version 4.0, with Replica Set name "r1".

From the admin shell, you then need to initiate the replica set:

```bash
mongo mongodb://localhost/ --eval "rs.initiate()"
```

When you have a local MongoDB instance running a replica set, you can build and run the example.

```powershell
dotnet run .\bin\Debug\netcoreapp2.1\mongodb-transactions-csharp.dll
```
