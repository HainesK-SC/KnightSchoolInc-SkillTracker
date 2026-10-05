using Capstone.Identity.API.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Capstone.Identity.API.Tests.Enums
{
    [TestClass]
    public class DisplayNameModifiersExtensionTests
    {
        private List<DisplayNameModifiers> _modifiers = new();
        private List<string> _modifierValues = new();

        [TestInitialize]
        public void TestInitialize()
        {
            _modifiers = Enum.GetValues<DisplayNameModifiers>().ToList();

            _modifierValues.Add("the Brave");
            _modifierValues.Add("the Bold");
            _modifierValues.Add("the Wise");
            _modifierValues.Add("the Swift");
            _modifierValues.Add("the Steadfast");
            _modifierValues.Add("the Valiant");
        }

        [TestMethod]
        public void InvalidNumericEnumValueThrowsExeption()
        {
            var undefinedValue = (DisplayNameModifiers)99;

            Assert.Throws<ArgumentOutOfRangeException>(() => undefinedValue.ToDisplayText());
        }

        [TestMethod]
        public void ValidNumericValueReturnsCorrectEnum()
        {
            var definedValue = (DisplayNameModifiers)0;

            var expectedValue = "the Brave";

            var actualValue = definedValue.ToDisplayText();

            Assert.AreEqual<string>(expectedValue, actualValue);
        }

        [TestMethod]
        [DataRow(DisplayNameModifiers.TheBrave, "the Brave")]
        [DataRow(DisplayNameModifiers.TheBold, "the Bold")]
        [DataRow(DisplayNameModifiers.TheWise, "the Wise")]
        [DataRow(DisplayNameModifiers.TheSwift, "the Swift")]
        [DataRow(DisplayNameModifiers.TheSteadfast, "the Steadfast")]
        [DataRow(DisplayNameModifiers.TheValiant, "the Valiant")]
        public void AllEnumValuesConvertToString(DisplayNameModifiers modifier, string expected)
        {
            Assert.AreEqual(expected, modifier.ToDisplayText());
        }
    }
}
