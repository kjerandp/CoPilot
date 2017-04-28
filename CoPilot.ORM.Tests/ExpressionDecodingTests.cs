using System;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class ExpressionDecodingTests
    {
       

        [TestMethod]
        public void CanDecodeSimpleExpressionForId()
        {
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.Id == 1));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.Id == 1 && (org.CountryCode == null || org.CountryCode == "NO")));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.HostNames != null));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(org => org.HostNames != null && org.City.Id > 10));
        }

        [TestMethod]
        public void CanDecodeMemberMetodExpressions()
        {
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.StartsWith("Ko", StringComparison.OrdinalIgnoreCase)));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.ToLower() == "x"));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.ToUpper() == "x"));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Name.Contains("rør")));
            Console.WriteLine(ExpressionHelper.DecodeExpression<Organization>(r => r.Id.ToString() == "x"));
        }

        [TestMethod]
        public void CanUseShortHandBooleansInExpressions()
        {
            var istrue = true;
            var f = ExpressionHelper.DecodeExpression<Organization>(r => istrue);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => !istrue);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue == true);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => r.Active);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => !r.Active);
            Console.WriteLine(f);
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue || r.Active);
            Console.WriteLine(f);

            istrue = false;
            f = ExpressionHelper.DecodeExpression<Organization>(r => istrue);
            Console.WriteLine(f);
        }
    }
}
