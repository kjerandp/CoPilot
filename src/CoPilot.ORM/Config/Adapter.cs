using CoPilot.ORM.Config.DataTypes;

namespace CoPilot.ORM.Config
{
    /// <summary>
    /// Delegate for doing data transformations of values to or from the database 
    /// </summary>
    /// <param name="target">Indicates if the transformation is for the database or the object</param>
    /// <param name="value">The value to transform</param>
    /// <returns>The transformed value</returns>
    public delegate object ValueAdapter(MappingTarget target, object value);
    
}
