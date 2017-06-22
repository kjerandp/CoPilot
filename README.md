# CoPilot

CoPilot is an object relational mapper (ORM). In its core it works like a micro ORM, but it has some neat and powerful features added on top of it, which makes it more of a "middle ground" ORM.  CoPilot is built with performance and flexibillity in mind, and was specifically designed to avoid any leakage into other (non-data access) layers. 

**Key features:**  
* Map POCO models to tables including relationships (one-to-many and many-to-one relationships). 
* Writes and executes SQL statements for CRUD operations. Related entities can be included for all operations, as long as they have a singular key defined.
* Partial update of entities using the `Patch` function.  
* Mapping of data from queries or stored procedures to dynamic objects or POCO classes. 
* Fluent query and configuration syntax (lambda functions).
* Projecting of data to anonymous or defined classes (only referenced columns will be included in the queries)
* Solves the "1+N"-problem by fetching child records in a single query and then merging the data with the parent records when mapped
* Perform mulitple write operations, including bulk commands, as a unit-of-work (database transaction)
* Transform values from and to the database by associating a `ValueAdapter` to relevant POCO properties. Examples of use cases is to serialize/deserialize to and from json, joining/splitting collections of primitive values, converting from/to enums etc.
* Use lookup tables - meaning that the value of a property can be used to lookup a key value in another table, and then pass that value to the mapped table and then do the same in reverse. Can be handy if you for instance want to use enums in your POCOs, but you want to enforce a foreign key constraint to another table for the mapped column.
* Generate scripts to build database from model configurations
* Support for multiple Ado.Net providers (currently implementations for Ms Sql Server and MySql exists)
* Validate configuration against database schema

## Install
Install the database provider you want to use:

```
Install-Package CoPilot.ORM.SqlServer -Pre
```
or

```
Install-Package CoPilot.ORM.MySql -Pre
```
## How to use
CoPilot aims to be as simple and intuitive as possible to use and most features are available from a single interface called `IDb`. Please look for examples in the test projects, especially the [BandSampleTests.cs](/test/CoPilot.ORM.IntegrationTests/BandSampleTests.cs).

### Quick examples

Showing most of the functions available when writing context queries with the new syntax introduced in v2:
```
var bands = _db.From<Band>()
    .Where(r => !r.Name.StartsWith("B") && !r.Name.StartsWith("L")) 
    .Include("BandMembers")
    .OrderBy(r => r.Name)
    .ThenBy(r => r.Formed, Ordering.Descending)
    .ThenBy(r => r.Id)
    .Skip(1)
    .Take(20)
    .Distinct()
    .AsEnumerable();
```
Mapping result to anonymous type (projection):
```
var bands = _db.From<Band>()
    .Select(r => new { 
        BandId = r.Id, 
        BandName = r.Name, 
        Nationality = r.Based.Country.Name 
    })
    .OrderBy(r => r.Nationality)
    .Take(50)
    .ToArray();
```
More complex projection:
```
var band = _db.From<Band>().Where(r => r.Id == 5).Select(r => 
    new {
        BandName = r.Name,
        Discography = r.Recordings.SelectMany(b => b.AlbumTracks.Select(t => 
        new {
            Album = t.Album.Title,
            t.Album.Released,
            Song = b.SongTitle,
            b.Recorded,
            t.TrackNumber,
            Duration = $"{b.Duration.Minutes}:{b.Duration.Seconds}"   
        })).OrderBy(x => x.Album).ThenBy(x => x.TrackNumber)
    }
).Single();
```

Executing and mapping stored procedure:
```
var recordings = _db.Query<Recording>(
    "Get_Recordings_CTE",                           //stored procedure name
    new { recorded = new DateTime(2017, 5, 1) },    //arguments (for parameter @recorded)
    "Base", "Base.AlbumTracks"                      //naming of record sets
);
```
Inserting new record with transaction support:
```
using (var writer = new DbWriter(_db))
{
    try {
        var testBand = new Band
        {
            Formed = DateTime.Today,
            Name = "Test Band",
            Based = writer.GetReader().FindByKey<City>(1)
        };

        writer.Save(testBand);
        
        (...)
        
        writer.Commit();
    } catch (Exception ex){
        writer.Rollback();
        //error handling
    }
}
```
Bulk inserting example:
```
const int insertCount = 10000;

using (var writer = new DbWriter(_db))
{
    var dt = new DateTime(1980,1,1);
    
    writer.PrepareCommand(@"
        insert into BAND (
            city_id,
            band_name,
            band_formed
        ) values (
            @cityId, 
            @bandName, 
            @formed
        )", 
        new {cityId=0, bandName=string.Empty, formed=dt}
    );

    for (var i = 0; i < insertCount; i++)
    {
        writer.Command(new {cityId = 1, bandName = "Lazy Bulk Band " + i, formed = dt.AddDays(i)});
    }

    writer.Commit();
}

```
You can also write your own sql, with or without configuration. If you don't provide a configuration, you can still map data by using either the `BasicMapper` or the `DynamicMapper` (or write your own mapping delegate). Both have settings/parameters that you can use to control how it maps from column names to object properties (case convertion, masking etc.).
```
var rowsUpdated = _db.Command(
    "UPDATE BAND SET BAND_NAME=@Name WHERE BAND_ID=@Id", 
    band   //using an instance of Band to pass arguments
);            
            
var updatedBand = _db.Query<Band>(
    "SELECT * FROM BAND WHERE BAND_ID=@Id", 
    new {band.Id}  //using an anonymous object to pass parameters 
).Single();     

var count = _db.Scalar<int>("SELECT COUNT(*) FROM BAND"); 

```
You can do the same thing using a single connection by using the `DbWriter` class, which also gives you
transaction support:
```
using (var writer = new DbWriter(_db))
{
    var reader = writer.GetReader(); //for queries
    var rowsUpdated = writer.Command(
        "UPDATE BAND SET BAND_NAME=@name WHERE BAND_ID=@band_id", 
        new { band_id = 1, name = "Muse" }
    );

    var updatedBand = reader.Query<Band>(
        "SELECT * FROM BAND WHERE BAND_ID=@id", 
        new { id = 1 }
    ).Single();

    var count = reader.Scalar<int>(
        "SELECT COUNT(*) FROM BAND"
    );

    writer.Commit();
}
```
If you only need to do queries, you can achieve the same thing by simply using the `DbReader`:
```
using (var reader = new DbReader(_db))
{
    var band = reader.FindByKey<Band>(1);

    var updatedBand = reader.Query<Band>("SELECT * FROM BAND WHERE BAND_ID=@Id", band).Single();

    var count = reader.Scalar<int>("SELECT COUNT(*) FROM BAND");

    var songs = reader.From<Recording>()
        .Where(r => r.Band.Id == band.Id)
        .Select(r => r.SongTitle)
        .Distinct()
        .ToArray();
}
```

## Configuration
The above examples has the following configuration:
```
public static DbModel CreateModel()
{
    // CoPilot class for mapping entities
    var mapper = new DbMapper();            

    // Use this to set an existing naming convention or create your own.
    // The default will name column in upper snake case. 
    // Column names are prefixed with its table name. To use class names
    // based on class and property names (Camel cased), you can use the built 
    // in convention `DbColumnNamingConvention.SameAsClassMemberNames`.  
    mapper.SetColumnNamingConvention(DbColumnNamingConvention.Default);

    // Class to table mappings
    mapper.Map<Country>("COUNTRY");         
    mapper.Map<MusicGenre>("MUSIC_GENRE");
    mapper.Map<Album>("ALBUM");

    var cityMap = mapper.Map<City>("CITY");
    var personMap = mapper.Map<Person>("PERSON");
    var bandMap = mapper.Map<Band>("BAND");
    var bandMemberMap = mapper.Map<BandMember>("BAND_MEMBER");
    var recordingMap = mapper.Map<Recording>("RECORDING");
    var albumTrackMap = mapper.Map<AlbumTrack>("ALBUM_TRACK");

    // Relationship mapping
    cityMap.HasOne(r => r.Country, "~COUNTRY_ID").InverseKeyMember(r => r.Cities);

    personMap.HasOne(r => r.City, "~CITY_ID");

    bandMap.HasOne(r => r.Based, "CITY_ID");

    bandMemberMap.HasOne(r => r.Person, "~PERSON_ID");
    bandMemberMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.BandMembers);

    recordingMap.HasOne(r => r.Genre, "~GENRE_ID").InverseKeyMember(r => r.Recordings);
    recordingMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.Recordings);

    albumTrackMap.HasOne(r => r.Recording, "~RECORDING_ID").InverseKeyMember(r => r.AlbumTracks);
    albumTrackMap.HasOne(r => r.Album, "~ALBUM_ID").InverseKeyMember(r => r.Tracks);

    // Creates an in-memory description of the database. Naming conventions and various assumptions 
    // are made to fill out the blanks unless specified by config.   
    return mapper.CreateModel();
}
```
The model can be used to create an instance of `IDb`:

```
var db = _model.CreateDb(<connectionstring>, <dbprovider>);
``` 
This instance should be put in a static context or serve as a singleton if using IoC containers.

The `DbModel` object can also be used with the `ScriptBuilder` class to generate various scripts, like
dropping and creating the database. 

