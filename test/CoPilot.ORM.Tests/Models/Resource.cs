namespace CoPilot.ORM.Tests.Models
{
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Organization Owner { get; set; }
        public Organization UsedBy { get; set; }

    }
}
