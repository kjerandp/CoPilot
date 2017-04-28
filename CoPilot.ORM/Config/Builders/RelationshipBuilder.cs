using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public class RelationshipBuilder<TFrom, TTo> : BaseBuilder where TFrom : class where TTo : class
    {
        private readonly DbRelationship _relationship;

        internal RelationshipBuilder(DbModel model, DbRelationship relationship):base(model)
        {
            _relationship = relationship;
        }

        public RelationshipBuilder<TFrom, TTo> IsRequired()
        {
            _relationship.ForeignKeyColumn.IsNullable = false;
            return this;
        }

        public RelationshipBuilder<TFrom, TTo> IsOptional()
        {
            _relationship.ForeignKeyColumn.IsNullable = true;
            return this;
        }

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