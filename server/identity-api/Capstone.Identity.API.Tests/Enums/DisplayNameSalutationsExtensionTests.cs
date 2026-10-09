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
        [DataRow(DisplayNameSalutations.Knight, "Knight")]
        [DataRow(DisplayNameSalutations.Sir, "Sir")]
        [DataRow(DisplayNameSalutations.Madame, "Madame")]
        public void AllEnumValuesConvertToString(DisplayNameSalutations salutation, string expected)
        {
            Assert.AreEqual(expected, salutation.ToDisplayText());
        }
    }
}
