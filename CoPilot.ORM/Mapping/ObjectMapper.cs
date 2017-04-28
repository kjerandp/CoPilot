using System.Collections.Generic;
using CoPilot.ORM.Database.Commands;

namespace CoPilot.ORM.Mapping
{
    public delegate MappedRecord[] ObjectMapper(DbRecordSet recordset);

    //public static class DataMapper
    //{

    //    public static IEnumerable<T> MapToModel<T>(DbRecordSet dataset, ObjectMapper mapper = null)
    //    {
    //        return MapToModel(typeof(T), dataset, mapper).OfType<T>();
    //    }

    //    public static IEnumerable<object> MapToModel(Type modelType, DbRecordSet dataset, ObjectMapper mapper = null)
    //    {
    //        if (mapper == null)
    //        {
    //            mapper = OptimisticMapper.Create(modelType);
    //        }
    //        return mapper.Invoke(dataset).Select(r => r.Instance);
    //    }
    //}
}
