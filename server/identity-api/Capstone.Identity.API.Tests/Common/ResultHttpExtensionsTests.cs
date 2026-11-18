using Capstone.Identity.API.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Identity.API.Tests.Common
{
    [TestClass]
    public class ResultHttpExtensionsTests
    {
        private const string ServiceMessage = "Something specific the service said.";
        private const string HiddenMessage = "An unexpected error occurred.";

        private static ProblemDetails GetProblemDetails(ObjectResult objectResult)
        {
            Assert.IsInstanceOfType<ProblemDetails>(objectResult.Value);
            return (ProblemDetails)objectResult.Value!;
        }

        // ---------- Status code mapping ----------

        [TestMethod]
        [DataRow(ResultErrorType.Validation, StatusCodes.Status400BadRequest)]
        [DataRow(ResultErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
        [DataRow(ResultErrorType.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(ResultErrorType.Conflict, StatusCodes.Status409Conflict)]
        [DataRow(ResultErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
        public void MapsErrorType_ToStatusCode(ResultErrorType errorType, int expectedStatus)
        {
            var result = Result<string>.Failure(ServiceMessage, errorType);

            var objectResult = result.ToProblemResult();

            Assert.AreEqual(expectedStatus, objectResult.StatusCode);
        }

        [TestMethod]
        [DataRow(ResultErrorType.Validation)]
        [DataRow(ResultErrorType.Unauthorized)]
        [DataRow(ResultErrorType.NotFound)]
        [DataRow(ResultErrorType.Conflict)]
        [DataRow(ResultErrorType.Unexpected)]
        public void ProblemDetailsStatus_MatchesHttpStatus(ResultErrorType errorType)
        {
            var result = Result<string>.Failure(ServiceMessage, errorType);

            var objectResult = result.ToProblemResult();

            var problem = GetProblemDetails(objectResult);
            Assert.AreEqual(objectResult.StatusCode, problem.Status);
        }

        // ---------- Message handling ----------

        [TestMethod]
        [DataRow(ResultErrorType.Validation)]
        [DataRow(ResultErrorType.Unauthorized)]
        [DataRow(ResultErrorType.NotFound)]
        [DataRow(ResultErrorType.Conflict)]
        public void ExpectedFailures_PassServiceMessage_ThroughAsDetail(ResultErrorType errorType)
        {
            var result = Result<string>.Failure(ServiceMessage, errorType);

            var problem = GetProblemDetails(result.ToProblemResult());

            Assert.AreEqual(ServiceMessage, problem.Detail);
        }

        [TestMethod]
        public void Unexpected_HidesServiceMessage()
        {
            // The service message on a 500 could contain internal details,
            // so the client only ever sees the generic text
            var result = Result<string>.Failure("SQL timeout on server db-01", ResultErrorType.Unexpected);

            var problem = GetProblemDetails(result.ToProblemResult());

            Assert.AreEqual(HiddenMessage, problem.Detail);
            Assert.IsFalse(problem.Detail!.Contains("db-01"));
        }

        // ---------- Edge cases ----------

        [TestMethod]
        public void SuccessResult_CalledByMistake_FallsBackTo500_WithHiddenMessage()
        {
            // Controllers should only call ToProblemResult on failures.
            // If one ever calls it on a success (ErrorType None), it must not
            // claim success or leak anything: it falls through to 500.
            var result = Result<string>.Success("data");

            var objectResult = result.ToProblemResult();

            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.AreEqual(HiddenMessage, GetProblemDetails(objectResult).Detail);
        }

        [TestMethod]
        public void UndefinedErrorType_FallsBackTo500()
        {
            var result = Result<string>.Failure(ServiceMessage, (ResultErrorType)99);

            var objectResult = result.ToProblemResult();

            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.AreEqual(HiddenMessage, GetProblemDetails(objectResult).Detail);
        }

        [TestMethod]
        public void WorksForAnyResultType()
        {
            // The extension is generic: same behaviour regardless of T
            var boolResult = Result<bool>.Failure(ServiceMessage, ResultErrorType.Conflict);
            var listResult = Result<IReadOnlyList<int>>.Failure(ServiceMessage, ResultErrorType.Conflict);

            Assert.AreEqual(StatusCodes.Status409Conflict, boolResult.ToProblemResult().StatusCode);
            Assert.AreEqual(StatusCodes.Status409Conflict, listResult.ToProblemResult().StatusCode);
        }
    }
}