using System;
using System.Collections.Generic;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class FilterExpressionWriter : IFilterExpressionWriter
    {
        public string GetExpression(FilterGraph filter, List<DbParameter> parameters, Dictionary<string, object> args)
        {
            return GetFilterOperandText(filter.Root, parameters, args);
        }

        private string GetFilterOperandText(IExpressionOperand operand, List<DbParameter> parameters, Dictionary<string, object> args)
        {
            var bo = operand as BinaryOperand;
            if (bo != null)
            {
                var str = $"{GetFilterOperandText(bo.Left, parameters, args)} {bo.Operator} {GetFilterOperandText(bo.Right, parameters, args)}";
                if (bo.Enclose)
                {
                    str = $"({str})";
                }
                return str;
            }

            var vo = operand as ValueOperand;

            if (vo != null)
            {
                parameters.Add(new DbParameter(vo.ParamName, DbConversionHelper.MapToDbDataType(vo.Value.GetType())));
                args.Add(vo.ParamName, vo.Value);
            }

            return operand.ToString(); //TODO option to parameterize or not
        }

        
    }
}