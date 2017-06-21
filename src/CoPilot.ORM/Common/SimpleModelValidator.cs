using System;
using System.Linq;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Common
{
    public class SimpleModelValidator : IModelValidator
    {
        public bool Validate(IDb db)
        {
            var isValid = true;
            foreach (var dbTable in db.Model.Tables)
            {
                Console.WriteLine(FormatMessage($"Validation of table: {dbTable}"));

                try
                {
                    var sql = db.DbProvider.CommonScriptingTasks.GetModelValidationScript(dbTable).ToString();
                    var res = db.Query(sql, null);
                    if (res.RecordSets.Last().Records.Length == 0)
                    {
                        Console.WriteLine(FormatMessage("> No data in table!",
                            "No errors, but unable to validate columns and data types."));
                        Console.WriteLine();
                        continue;
                    }
                    var fieldNames = res.RecordSets.Last().FieldNames;
                    var types = res.RecordSets.Last().FieldTypes;
                    for (var f = 0; f < fieldNames.Length; f++)
                    {
                        var field = new {Name = fieldNames[f], Type = DbConversionHelper.MapToDbDataType(types[f])};
                        var modelCol =
                            dbTable.Columns.FirstOrDefault(
                                r => r.ColumnName.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

                        if (modelCol == null)
                        {
                            Console.WriteLine(FormatMessage($"> '{field.Name}'",
                                $"The column '{field.Name}' is not mapped."));
                        }
                        else if (modelCol.DataType != field.Type)
                        {

                            Console.WriteLine(FormatMessage($"> '{field.Name}'",
                                $"The column '{field.Name}' is of type {field.Type}, but mapped to {modelCol.DataType}"));
                        }
                        else
                        {
                            Console.WriteLine(FormatMessage($"> '{field.Name}' OK"));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(FormatMessage("FAILED", e.Message));
                    isValid = false;
                }
                Console.WriteLine();
            }

            return isValid;
        }

        private string FormatMessage(string msg, string details = null)
        {
            var block = new ScriptBlock();

            block.Add(msg);
            if (details != null)
            {
                block.Add("Details:");
                block.Add(new ScriptBlock(details.Split('\n')));
            }
            return block.ToString();
        }
    }
}
