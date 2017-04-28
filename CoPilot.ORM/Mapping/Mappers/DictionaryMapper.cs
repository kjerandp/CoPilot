namespace CoPilot.ORM.Mapping.Mappers
{
    public static class DictionaryMapper
    {
      
        public static ObjectMapper Create()
        {
            ObjectMapper mapper = dataset =>
            {
                var result = new MappedRecord[dataset.Records.Length];
                var i = 0;
                foreach (var item in dataset.AsEnumerable())
                {
                    result[i] = new MappedRecord(item);
                    i++;
                }
                
                return result;
            };

            return mapper;
        }
        

       
    }
}
