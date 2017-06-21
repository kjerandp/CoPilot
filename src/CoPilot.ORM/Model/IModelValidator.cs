namespace CoPilot.ORM.Model
{
    public interface IModelValidator
    {
        bool Validate(IDb db);
    }
}
