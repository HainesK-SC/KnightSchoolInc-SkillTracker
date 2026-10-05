using System;
using System.Collections.Generic;
using System.Text;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Microsoft.Identity.Client;
using Moq;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class DisplayNameGeneratorTests
    {
        private Mock<IDisplayNameGenerator> _moqDisplayNameGenerator;
        private IDisplayNameGenerator _nameGenerator;

        [TestInitialize] 
        public void Initialize() 
        {
            _moqDisplayNameGenerator = new Mock<IDisplayNameGenerator>();
            _nameGenerator = new DisplayNameGenerator();
        }
            
        [TestMethod]
        public async Task GenerateDisplayNameGeneratesCorrectDisplayName()
        {
            var expectedResult = "Sir, Kyle the Brave";

            var actualResult = _nameGenerator.GenerateDisplayName(API.Enums.DisplayNameSalutations.Sir, "Kyle", API.Enums.DisplayNameModifiers.TheBrave);

            Assert.AreEqual<string>(expectedResult, actualResult.Result);
        }

        [TestMethod]
        public async Task GenerateDisplayNameTrimsFirstName()
        {
            var expectedResult = "Sir, Kyle the Brave";

            var actualResult = _nameGenerator.GenerateDisplayName(API.Enums.DisplayNameSalutations.Sir, "    Kyle  ", API.Enums.DisplayNameModifiers.TheBrave);

            Assert.AreEqual<string>(expectedResult, actualResult.Result);
        }

        [TestMethod]
        public async Task PickRandomModifierReturnsDefinedEnumValues()
        {
            var randInt = new Random().Next(10);
            var counter = 0;

            var modifiersList = Enum.GetValues<DisplayNameModifiers>().ToList();

            while (counter < randInt)
            {
                var enumValue = _nameGenerator.PickRandomModifier();
                Assert.Contains(enumValue, modifiersList);
                counter++;
            }
        }

        [TestMethod]
        public async Task DefaultSalutationIsCorrect()
        {
            var expectedResult = DisplayNameSalutations.Knight;

            var actualResult = _nameGenerator.DefaultSalutation;

            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
