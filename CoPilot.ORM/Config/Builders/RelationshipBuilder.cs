using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    /// <summary>
    /// Builder class to configure relationship specific settings
    /// </summary>
    /// <typeparam name="TFrom">Mapped POCO</typeparam>
    /// <typeparam name="TTo">Mapped POCO</typeparam>
    public class RelationshipBuilder<TFrom, TTo> : BaseBuilder where TFrom : class where TTo : class
    {
        private readonly DbRelationship _relationship;

        internal RelationshipBuilder(DbModel model, DbRelationship relationship):base(model)
        {
            _relationship = relationship;
        }

        /// <summary>
        /// Explicitly set that the relationship is required (force INNER JOINS and NON NULLABLE keys)
        /// </summary>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<TFrom, TTo> IsRequired()
        {
            _relationship.ForeignKeyColumn.IsNullable = false;
            return this;
        }

        /// <summary>
        /// Explicitly set that the relationship is not required (force LEFT JOINS and NULLABLE keys)
        /// </summary>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<TFrom, TTo> IsOptional()
        {
            _relationship.ForeignKeyColumn.IsNullable = true;
            return this;
        }

        /// <summary>
        /// Connect a navigation property in the POCO class that is mapped to the entitiy with the foreign key
        /// when a primitive property is used to define the relationship
        /// </summary>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<TFrom, TTo> KeyForMember(Expression<Func<TFrom, TTo>> prop)
        {
            if (prop != null)
            {
                var keyFor = ExpressionHelper.GetMemberInfoFromExpression(prop);
                var map = Model.GetTableMap<TFrom>();
                map.MapMemberToRelationship(keyFor, _relationship);
            }
            return this;
        }

        /// <summary>
        /// Connect a navigation property in the POCO class that is mapped to the entitiy with the primary key
        /// <remarks>Must be a collection of type TFrom</remarks>
        /// </summary>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<TFrom, TTo> InverseKeyMember(Expression<Func<TTo, ICollection<TFrom>>> prop)
        {
            if (prop != null)
            {
                var inverseKey = ExpressionHelper.GetMemberInfoFromExpression(prop);
                var map = Model.GetTableMap<TTo>();
                map.MapMemberToRelationship(inverseKey, _relationship);
            }
            return this;
        }

        /// <summary>
        /// Used when you need to join on a key that is different than the primary key 
        /// <remarks>The key must have a unique index constraint</remarks>
        /// </summary>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<TFrom, TTo> JoinOnKey(Expression<Func<TTo, object>> key)
        {
            var prop = ExpressionHelper.GetMemberInfoFromExpression(key);
            var map = Model.GetTableMap<TTo>();
            var col = map.GetColumnByMember(prop);
            
            if(col == null || !col.Unique) throw new ArgumentException("Selected column does not exist or is not configured to be unique!");
            _relationship.ChangePrimaryKeyTo(col);
            return this;
        }
    }
}