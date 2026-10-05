using System;
using System.Collections.Generic;
using System.Text;
using Capstone.Identity.API.Repositories;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class DisplayNameGeneratorTests
    {
        private Mock<IDisplayNameGenerator> _moqDisplayNameGenerator;

        [TestInitialize] 
        public void Initialize() 
        {
            _moqDisplayNameGenerator = new Mock<IDisplayNameGenerator>();
        }

        //[TestMethod]
        //public async Task GenerateDisplayNameValidFirstNameReturnsDisplayName()
        //{

        //}
    }
}
