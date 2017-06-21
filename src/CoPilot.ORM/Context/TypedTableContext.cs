using System;
using System.Linq.Expressions;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Context.Query.Selector;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public class TableContext<T> : TableContext where T : class
    {
        public TableContext(DbModel model, params string[] include) : base(model, typeof(T), include) { }

        public void ApplySelector<TTarget>(Expression<Func<T, TTarget>> selector)
        {
            ProcessSelectorExpression(selector);
        }

        public void ApplySelector(Expression<Func<T, object>> selector)
        {
            ProcessSelectorExpression(selector);
        }

        private void ProcessSelectorExpression(Expression selector)
        {
            var decoder = new SelectExpressionProcessor(this);
            
            SelectTemplate = decoder.Decode(selector);

            
        }
        
        public OperationContext Insert(T entity)
        {
            return Insert(this, entity);
        }

        public OperationContext Delete(T entity)
        {
            return Delete(this, entity);
        }

        public OperationContext Update(T entity)
        {
            return Update(this, entity);
        }


    }
}
