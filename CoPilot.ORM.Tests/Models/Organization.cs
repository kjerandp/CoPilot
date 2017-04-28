using System.Collections.Generic;

namespace CoPilot.ORM.Tests.Models
{
    public class Organization 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CityCode { get; set; }
        public List<string> HostNames { get; set; }
        public int CityId { get; set; }
        public City City { get; set; }
        public List<Resource> OwnedResources { get; set; }
        public List<Resource> UsedResources { get; set; }
        public string CountryCode { get; set; }
        public OrganizationType OrganizationType { get; set; }
        public bool Active { get; set; }
    }

    public enum OrganizationType
    {
        TypeA,
        TypeB,
        TypeC
    }
}
