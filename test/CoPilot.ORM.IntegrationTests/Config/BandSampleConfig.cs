using CoPilot.ORM.Config;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using CoPilot.ORM.Model;
using CoPilot.ORM.Providers.SqlServer;

namespace CoPilot.ORM.IntegrationTests.Config
{
    public class BandSampleConfig
    {
        private const string DefaultConnectionString = @"
                data source=localhost; 
                initial catalog=BANDS_SAMPLE_DB; 
                Integrated Security=true;
                MultipleActiveResultSets=True; 
                App=CoPilotIntegrationTest;";

        public static IDb CreateFromConfig(string connectionString)
        {
            return CreateModel().CreateDb(connectionString ?? DefaultConnectionString, new SqlServerProvider());
        }

        public static DbModel CreateModel()
        {
            var mapper = new DbMapper();

            var countryMap = mapper.Map<Country>("COUNTRY");
            var cityMap = mapper.Map<City>("CITY");
            var personMap = mapper.Map<Person>("PERSON");
            var genreMap = mapper.Map<MusicGenre>("MUSIC_GENRE");
            var bandMap = mapper.Map<Band>("BAND");
            var bandMemberMap = mapper.Map<BandMember>("BAND_MEMBER");
            var recordingMap = mapper.Map<Recording>("RECORDING");
            var albumMap = mapper.Map<Album>("ALBUM");
            var albumTrackMap = mapper.Map<AlbumTrack>("ALBUM_TRACK");

            cityMap.HasOne(r => r.Country, "~COUNTRY_ID").InverseKeyMember(r => r.Cities);

            personMap.HasOne(r => r.City, "~CITY_ID");

            bandMap.HasOne(r => r.Based, "CITY_ID");

            bandMemberMap.HasOne(r => r.Person, "~PERSON_ID");
            bandMemberMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.BandMembers);

            recordingMap.HasOne(r => r.Genre, "~GENRE_ID").InverseKeyMember(r => r.Recordings);
            recordingMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.Recordings);

            albumTrackMap.HasOne(r => r.Recording, "~RECORDING_ID").InverseKeyMember(r => r.AlbumTracks);
            albumTrackMap.HasOne(r => r.Album, "~ALBUM_ID").InverseKeyMember(r => r.Tracks);

            return mapper.CreateModel();
        }
    }
}
