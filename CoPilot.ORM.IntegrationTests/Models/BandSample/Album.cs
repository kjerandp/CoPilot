using System;
using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Released { get; set; }
        public List<AlbumTrack> Tracks { get; set; }
        
    }
}