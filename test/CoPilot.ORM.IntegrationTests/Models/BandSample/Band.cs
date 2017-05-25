using System;
using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class Band
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<BandMember> BandMembers { get; set; }
        public DateTime Formed { get; set; }
        public City Based { get; set; }
        public List<Recording> Recordings { get; set; }
    }
}