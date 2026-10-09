using Capstone.Identity.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Capstone.Identity.API.Tests.Models
{
    [TestClass]
    public class RolesTests
    {
        [TestMethod]
        public async Task GetAllRolesReturnsAllRoles()
        {
            var expectedResult = new List<string>
            {
                "ADMINISTRATOR",
                "REGULAR_USER",
                "INSTRUCTOR"
            };

            var expectedCount = 3;

            var actualResult = Roles.GetAllRolesAsync().Result;

            var actualCount = actualResult.Count();

            Assert.AreEqual(expectedCount, actualCount);

            for (int i = 0; i < expectedResult.Count(); i++)
            {
                Assert.AreEqual<string>(expectedResult[i], actualResult[i]);
            }

        }
    }
}
