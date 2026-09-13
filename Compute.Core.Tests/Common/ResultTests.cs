using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;

namespace Compute.Core.Tests.Common
{
    public class ResultTests
    {
        [Fact]
        public void AValue_IsSuccessful_AndReadsBack()
        {
            Result<int, FaultCode> result = 42;

            Assert.True(result.IsSuccessful);
            Assert.Equal(42, result.Value);
            Assert.Null(result.Error);
        }

        [Fact]
        public void AnError_IsNotSuccessful_AndCarriesTheCode()
        {
            Result<int, FaultCode> result = FaultCode.PermissionException;

            Assert.False(result.IsSuccessful);
            Assert.Equal(FaultCode.PermissionException, result.Error);
        }

        [Fact]
        public void ReadingTheValueOfAFailure_Throws()
        {
            Result<int, FaultCode> result = new(FaultCode.FeatureNotSupported);

            var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
            Assert.Contains("FeatureNotSupported", ex.Message);
        }

        [Fact]
        public void ANullReferenceValue_IsStillASuccess()
        {
            // "No error" and "no value" are different answers: a service may legitimately
            // succeed with null, and that must not read as a failure.
            Result<string?, FaultCode> result = (string?)null;

            Assert.True(result.IsSuccessful);
            Assert.Null(result.Value);
        }

        [Fact]
        public void DefaultBool_IsNotMistakenForFailure()
        {
            Result<bool, FaultCode> result = false;

            Assert.True(result.IsSuccessful);
            Assert.False(result.Value);
        }
    }
}
