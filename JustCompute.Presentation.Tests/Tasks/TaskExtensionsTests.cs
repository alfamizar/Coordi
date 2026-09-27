using JustCompute.Presentation.Tasks;

namespace JustCompute.Presentation.Tests.Tasks
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task AFailureIsReported_WithItsOwnException_NotTheWrapper()
        {
            var reported = new TaskCompletionSource<Exception>();

            Task.Run(() => throw new InvalidOperationException("boom"))
                .Forget(ex => reported.TrySetResult(ex));

            var exception = await reported.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("boom", exception.Message);
        }

        [Fact]
        public void AnAlreadyFailedTaskIsReportedStraightAway()
        {
            Exception? reported = null;

            Task.FromException(new ArgumentException("late")).Forget(ex => reported = ex);

            Assert.IsType<ArgumentException>(reported);
        }

        [Fact]
        public async Task CancellationIsNotAFault()
        {
            // A load abandoned for a newer one is the normal case, not something to log.
            var reported = false;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Task.FromCanceled(cts.Token).Forget(_ => reported = true);
            Task.Delay(Timeout.Infinite, cts.Token).Forget(_ => reported = true);
            await Task.Delay(50);

            Assert.False(reported);
        }

        [Fact]
        public async Task SuccessIsSilent()
        {
            var reported = false;

            Task.CompletedTask.Forget(_ => reported = true);
            Task.Delay(10).Forget(_ => reported = true);
            await Task.Delay(50);

            Assert.False(reported);
        }
    }
}
