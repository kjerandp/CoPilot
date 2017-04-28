using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class HelperAndExtensionTests
    {
        [TestMethod]
        public void CanGetRemoveFirstPartOfPath()
        {
            Assert.AreEqual("b.c.d", PathHelper.RemoveFirstElementFromPathString("a.b.c.d"));
            Assert.AreEqual("bcd", PathHelper.RemoveFirstElementFromPathString("a.bcd"));
            Assert.AreEqual("d.e", PathHelper.RemoveFirstElementFromPathString("abc.d.e"));
            Assert.AreEqual("", PathHelper.RemoveFirstElementFromPathString("a"));
            Assert.IsNull(PathHelper.RemoveFirstElementFromPathString(null));
        }

        [TestMethod]
        public void CanSplitLastInPath()
        {
            Assert.AreEqual(new Tuple<string,string>("a.b.c","d"), PathHelper.SplitLastInPathString("a.b.c.d"));
            Assert.AreEqual(new Tuple<string, string>("", "abc"), PathHelper.SplitLastInPathString("abc"));
            Assert.AreEqual(new Tuple<string, string>("a", "b"), PathHelper.SplitLastInPathString("a.b"));
            Assert.IsNull(PathHelper.SplitLastInPathString(null));
        }

        [TestMethod]
        public void CanGetTypeFromCollections()
        {
            var t1 = new List<Organization>();
            Assert.AreEqual(typeof(Organization), t1.GetType().GetCollectionType());
            var t2 = (IEnumerable) t1;
            Assert.AreEqual(typeof(Organization), t2.GetType().GetCollectionType());
            var t3 = new Organization[1];
            Assert.AreEqual(typeof(Organization), t3.GetType().GetCollectionType());
            var t4 = new Collection<Organization>();
            Assert.AreEqual(typeof(Organization), t4.GetType().GetCollectionType());
        }

        [TestMethod]
        public void CanGetTypesFromPath()
        {

            var types = PathHelper.GetTypesFromPath(typeof(Organization), "City");
            Assert.AreEqual("City", string.Join(".", types.Select(r => r.Value.Name)));
            types = PathHelper.GetTypesFromPath(typeof(Resource), "Owner.City");
            Assert.AreEqual("Organization.City", string.Join(".", types.Select(r => r.Value.Name)));
            types = PathHelper.GetTypesFromPath(typeof(Resource), "Owner.City.CityCode",false);
            Assert.AreEqual("Organization.City.String", string.Join(".", types.Select(r => r.Value.Name)));
            types = PathHelper.GetTypesFromPath(typeof(Organization), "OwnedResources");
            try
            {
                PathHelper.GetTypesFromPath(typeof(Organization), "Owner");
                Assert.Fail("Should not happen!");
            }
            catch (Exception)
            {
                // ignored
            }


            Assert.AreEqual("List`1", string.Join(".", types.Select(r => r.Value.Name)));
            Assert.IsTrue(types["OwnedResources"].IsCollection());
        }

        [TestMethod]
        public void CanRemoveSimpleTypesFromPaths()
        {
            var types = PathHelper.RemoveSimpleTypesFromPaths(typeof(Resource), "Owner.City.CityCode", "Id", "UsedBy");
            Assert.AreEqual(2, types.Length);
            Assert.AreEqual("Owner.City", types[0]);
            Assert.AreEqual("UsedBy", types[1]);
        }
    }
}
