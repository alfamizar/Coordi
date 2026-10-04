using JustCompute.Presentation.Tasks;

namespace JustCompute.Presentation.Tests.Tasks
{
    public class SharedRequestTests
    {
        [Fact]
        public async Task CallersWhileARunIsOutShareIt()
        {
            var request = new SharedRequest<int>();
            var gate = new TaskCompletionSource<int>();
            var starts = 0;

            var calls = Enumerable.Range(0, 5)
                .Select(_ => request.RunAsync(() => { starts++; return gate.Task; }))
                .ToArray();
            Assert.True(request.IsRunning);

            gate.SetResult(42);
            var results = await Task.WhenAll(calls);

            Assert.Equal(1, starts);
            Assert.All(results, r => Assert.Equal(42, r));
            Assert.False(request.IsRunning);
        }

        [Fact]
        public async Task AFinishedRunIsNotHandedOut_TheNextCallerStartsAgain()
        {
            var request = new SharedRequest<int>();
            var starts = 0;

            await request.RunAsync(async () => { await Task.Yield(); return ++starts; });
            var second = await request.RunAsync(async () => { await Task.Yield(); return ++starts; });

            Assert.Equal(2, second);
        }

        [Fact]
        public async Task ARunThatEndsAtOnceDoesNotStick()
        {
            var request = new SharedRequest<int>();
            var starts = 0;

            await request.RunAsync(() => Task.FromResult(++starts));
            await request.RunAsync(() => Task.FromResult(++starts));

            Assert.Equal(2, starts);
            Assert.False(request.IsRunning);
        }

        [Fact]
        public async Task AFailureReachesEveryoneSharingTheRun_AndIsNotKept()
        {
            var request = new SharedRequest<int>();
            var gate = new TaskCompletionSource<int>();

            var first = request.RunAsync(() => gate.Task);
            var second = request.RunAsync(() => throw new InvalidOperationException("not used"));
            gate.SetException(new TimeoutException("no fix"));

            await Assert.ThrowsAsync<TimeoutException>(() => first);
            await Assert.ThrowsAsync<TimeoutException>(() => second);
            Assert.Equal(7, await request.RunAsync(() => Task.FromResult(7)));
        }
    }
}
