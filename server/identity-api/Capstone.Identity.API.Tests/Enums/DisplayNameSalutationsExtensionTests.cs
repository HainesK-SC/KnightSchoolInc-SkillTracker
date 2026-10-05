using Capstone.Identity.API.Enums;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;

namespace Capstone.Identity.API.Tests.Enums
{
    [TestClass]
    public class DisplayNameSalutationsExtensionTests
    {
        private DisplayNameSalutations[] _salutations = [];
        private string[] _salutationValues = [];

        [TestInitialize]
        public void Initialize()
        {
            _salutations = Enum.GetValues<DisplayNameSalutations>();

            _salutationValues.Append<string>("Knight");
            _salutationValues.Append<string>("Sir");
            _salutationValues.Append<string>("Madame");
        }

        [TestMethod]
        public void InvalidNumericEnumValueThrowsException()
        {
            var undefinedValue = (DisplayNameSalutations)99;

            Assert.Throws<ArgumentOutOfRangeException>(() => undefinedValue.ToDisplayText());
        }

        [TestMethod]
        public void ValidNumericEnumValueReturnsCorrectEnum()
        {
            var definedValue = (DisplayNameSalutations)0;

            var expectedValue = "Knight";

            var actualValue = definedValue.ToDisplayText();

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void AllEnumValuesConvertToString()
        {
            foreach (var salutation in _salutations)
            {
                Assert.IsInstanceOfType<string>(salutation.ToDisplayText());
            }
        }
    }
}
