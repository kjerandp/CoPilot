namespace CoPilot.ORM.Model
{
    public class DbRelationship
    {
        internal DbRelationship(DbColumn foreignKey, DbColumn primaryKey)
        {
            ForeignKeyColumn = foreignKey;
            PrimaryKeyColumn = primaryKey;
        }
        public DbColumn ForeignKeyColumn { get; }
        public DbColumn PrimaryKeyColumn { get; private set; }
        
        public bool IsLookupRelationship => LookupColumn != null;
        public DbColumn LookupColumn { get; internal set; }

        internal void ChangePrimaryKeyTo(DbColumn col)
        {
            PrimaryKeyColumn = col;
        }

        public override int GetHashCode()
        {
            return ForeignKeyColumn.Table.TableName.GetHashCode() ^ ForeignKeyColumn.GetHashCode();// ^ PrimaryKeyColumn.Table.TableName.GetHashCode() ^ PrimaryKeyColumn.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return GetHashCode() == obj?.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ForeignKeyColumn.Table.TableName} ({ForeignKeyColumn}) --> {PrimaryKeyColumn.Table.TableName} ({PrimaryKeyColumn})";
        }

        
    }
}
