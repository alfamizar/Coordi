using JustCompute.Presentation.Tasks;

namespace JustCompute.Presentation.Tests.Tasks
{
    /// <summary>
    /// "Superseded" has to mean one thing: a newer request cancelled this one. Anything else that
    /// happens to arrive as a cancellation is a failure, and must reach the caller as one.
    /// </summary>
    public class SupersedingTaskTests
    {
        [Fact]
        public async Task AFinishedRunIsNotSuperseded()
        {
            using var task = new SupersedingTask();

            var (superseded, result) = await task.RunAsync(_ => Task.FromResult(42));

            Assert.False(superseded);
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ANewerRunSupersedesTheOneInFlight()
        {
            using var task = new SupersedingTask();
            var gate = new TaskCompletionSource();

            var first = task.RunAsync(async token =>
            {
                await gate.Task.WaitAsync(token);
                return "first";
            });
            var second = task.RunAsync(_ => Task.FromResult("second"));

            Assert.Equal((true, (string?)null), await first);
            Assert.Equal((false, (string?)"second"), await second);
        }

        [Fact]
        public async Task ACancellationItDidNotAskForIsAFailure_NotASupersession()
        {
            // HttpClient reports its own timeout exactly like this: a TaskCanceledException whose
            // token is not the caller's. Swallowed as "superseded", the weather card published
            // nothing and its loading skeleton never went away.
            using var task = new SupersedingTask();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                task.RunAsync<string>(_ => throw new TaskCanceledException("The request timed out.")));
        }

        [Fact]
        public async Task AnOrdinaryFailureStillBelongsToTheCaller()
        {
            using var task = new SupersedingTask();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                task.RunAsync<int>(_ => throw new InvalidOperationException()));
        }
    }
}
