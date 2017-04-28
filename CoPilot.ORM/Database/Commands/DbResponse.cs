namespace CoPilot.ORM.Database.Commands
{
    public struct DbResponse
    {
        internal DbResponse(DbRecordSet[] results, long elapsedMs)
        {
            RecordSets = results;
            ElapsedMs = elapsedMs;
        }

        public long ElapsedMs { get; }
        public DbRecordSet[] RecordSets { get; }
    }
}