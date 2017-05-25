namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class AlbumTrack
    {
        public int Id { get; set; }
        public Recording Recording { get; set; }
        public Album Album { get; set; }
        public int TrackNumber { get; set; }

    }
}