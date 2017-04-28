using System;
using System.Linq;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Model;
using CoPilot.ORM.Tests.Models;

namespace CoPilot.ORM.Tests
{
    internal static class TestModel
    {
        internal static DbModel GetModel()
        {
            var mapper = new DbMapper();

            //mapper.AddLookupTable("GOT_TYPE", "~ID", "~PROGID");
            var tb = mapper.Map("GOT_TYPE");
            tb.HasKey("~ID", DbDataType.Int32).Alias("Id");
            tb.Column("~PROGID", DbDataType.String, "ProgId").MaxSize(255).Unique();
            tb.Column("~NAME", DbDataType.String, "Name").IsRequired().MaxSize(100);
            tb.Column("~ACTIVE", DbDataType.Boolean, "Active");
            tb.Column("~TABLE", DbDataType.String, "Table").IsRequired().MaxSize(50);
            tb.Column("~DESCRIPTION", DbDataType.Text, "Description").IsRequired();

            var cb = mapper.Map<City>("PUB_CITY");

            var rb = mapper.Map<Resource>("TST_RESOURCE");

            var ob = mapper.Map<Organization>("GOT_ORGANIZATION");
            ob.Column(r => r.HostNames);
            ob.SetValueAdapter(r => r.HostNames, 
                list => string.Join(";", list),
                s => s.ToString().Split(';').ToList()
                );
            ob.Column(r => r.OrganizationType, "GOT_TYPE_ID", "GOT_TYPE");
            ob.SetValueAdapter(r => r.OrganizationType,
                o => o.ToString().ToUpper(),
                d => (OrganizationType)Enum.Parse(typeof(OrganizationType), d.ToString(), true)
            );

            rb.HasOne(r => r.Owner, "~OWNER_ID").InverseKeyMember(r => r.OwnedResources).IsRequired();
            rb.HasOne(r => r.UsedBy, "~USEDBY_ID").InverseKeyMember(r => r.UsedResources).IsOptional();
            ob.HasOne<City>(r => r.CityId, "PUB_CITY_ID").KeyForMember(r => r.City).InverseKeyMember(r => r.Organizations).IsRequired();

            var me = mapper.Map<Media>("TST_MEDIA");
            me.Column(r => r.DataBytes).DataType(DbDataType.Varbinary).MaxSize(2*1024*1024);
            return mapper.CreateModel();
        }
    }
}