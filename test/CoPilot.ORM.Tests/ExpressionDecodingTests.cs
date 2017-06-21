using System;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.SqlServer;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class ExpressionDecodingTests
    {
        IDbProvider provider = new SqlServerProvider();

        [TestMethod]
        public void CanDecodeSimpleExpressionForId()
        {
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.Id == 1, provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.Id == 1 && (org.CountryCode == null || org.CountryCode == "NO"), provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.HostNames != null, provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.HostNames != null && org.City.Id > 10, provider));
        }

        [TestMethod]
        public void CanDecodeMemberMetodExpressions()
        {
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.StartsWith("Ko", StringComparison.OrdinalIgnoreCase), provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.ToLower() == "x", provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.ToUpper() == "x", provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.Contains("rør"), provider));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Id.ToString() == "x", provider));
        }

        [TestMethod]
        public void CanUseShortHandBooleansInExpressions()
        {
            var istrue = true;
            var f = ExpressionHelper.DecodeExpression<Organization>(r => istrue, provider);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => !istrue, provider);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue == true, provider);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => r.Active, provider);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => !r.Active, provider);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue || r.Active, provider);
            Console.WriteLine(f);

            istrue = false;
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue, provider);
            Console.WriteLine(f);
        }
    }
}
