using CoPilot.ORM.Database.Commands;

namespace CoPilot.ORM.Mapping
{
    public delegate MappedRecord[] ObjectMapper(DbRecordSet recordset);

}
