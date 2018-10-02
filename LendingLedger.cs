using System;
using MongoDB.Bson;

namespace Nuri.MongoDB.Transactions
{
    public class LendingLedger
    {
        public ObjectId Id;
        public DateTime CheckOutTime;
        public DateTime? CheckInTime;
        public int PersonId;
        public int ToolId;
    }

}