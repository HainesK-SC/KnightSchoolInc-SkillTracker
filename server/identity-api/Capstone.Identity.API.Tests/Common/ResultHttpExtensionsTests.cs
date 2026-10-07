using Capstone.Identity.API.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Capstone.Identity.API.Tests.Common
{
    [TestClass]
    public class ResultHttpExtensionsTests
    {
        [TestMethod]
        [DataRow(ResultErrorType.Validation, StatusCodes.Status400BadRequest)]
        [DataRow(ResultErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
        [DataRow(ResultErrorType.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(ResultErrorType.Conflict, StatusCodes.Status409Conflict)]
        [DataRow(ResultErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
        public void ToProblemResultMapsCorrectCode(ResultErrorType errorType,  int expectedStatusCode)
        {
            var result = Result<string>.Failure("Some error", errorType);

            var actionResult = result.ToProblemResult();

            Assert.AreEqual(expectedStatusCode, actionResult.StatusCode);

            var problem = actionResult.Value as ProblemDetails;
            Assert.IsNotNull(problem);
            Assert.AreEqual(expectedStatusCode, problem.Status);
        }

        [TestMethod]
        public void ToProblemResult_ServerError_HidesOriginalMessage()
        {
            var result = Result<string>.Failure("internal details", ResultErrorType.Unexpected);

            var problem = (ProblemDetails)result.ToProblemResult().Value!;

            Assert.AreEqual("An unexpected error occurred.", problem.Detail);
            Assert.IsFalse(problem.Detail!.Contains("internal details"));
        }

        [TestMethod]
        public void ToProblemResult_ClientError_PassesMessageThrough()
        {
            var result = Result<string>.Failure("Email already exists.", ResultErrorType.Conflict);

            var problem = (ProblemDetails)result.ToProblemResult().Value!;

            Assert.AreEqual("Email already exists.", problem.Detail);
        }
    }
}
