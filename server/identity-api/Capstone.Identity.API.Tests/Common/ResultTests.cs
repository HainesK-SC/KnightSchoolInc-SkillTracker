using Capstone.Identity.API.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Capstone.Identity.API.Tests.Common
{
    [TestClass]
    public class ResultTests
    {
        [TestMethod]
        public void ResultSuccessSetsProperValues()
        {
            var testData = 1;

            var successResult = Result<int>.Success(1);

            Assert.IsTrue(successResult.Succeeded);
            Assert.IsNotNull(successResult.Data);
            Assert.IsTrue(successResult.ErrorType == ResultErrorType.None);
        }

        [TestMethod]
        public void ResultFailureSetsProperValues()
        {
            var testError = "Operation failed.";
            var errorType = ResultErrorType.Unexpected;
            var failureResult = Result<int>.Failure(testError, errorType);

            Assert.IsFalse(failureResult.Succeeded);
            Assert.AreEqual(failureResult.Error, testError);
            Assert.IsNotNull(failureResult.Error);
            Assert.IsNotNull(failureResult.ErrorType);
            Assert.AreEqual(failureResult.ErrorType, errorType);
        }
    }
}
