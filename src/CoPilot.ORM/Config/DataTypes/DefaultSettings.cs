using System.Collections.Generic;

namespace CoPilot.ORM.Config.DataTypes
{
    public class DefaultSettings
    {
        public static Dictionary<SettingType, object> Get()
        {
            var settings = new Dictionary<SettingType, object>
            {
                { SettingType.DefaultVarcharSize, 255 },
                { SettingType.DefaultNumberPrecision, new NumberPrecision(10,2) },
                { SettingType.DefaultValueForPrimaryKeys, new DefaultValue(DbExpressionType.PrimaryKeySequence) }
            };

            return settings;
        }

        public static object Get(SettingType key)
        {
            var settings = Get();

            return settings.ContainsKey(key) ? settings[key] : null;
        }
    }
}
