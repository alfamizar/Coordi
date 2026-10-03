namespace JustCompute.Presentation.Tasks
{
    /// <summary>
    /// Runs work of which only the most recent request matters.
    ///
    /// Stepping the date or changing location fires a fresh fetch without waiting for the one
    /// already in flight, so two overlap and the slower one must not publish over the newer. The
    /// same holds for re-sorting search results. The bookkeeping is identical in every case while
    /// what each caller does with the answer is not, so only the bookkeeping lives here.
    /// </summary>
    public sealed class SupersedingTask : IDisposable
    {
        private CancellationTokenSource? _current;

        /// <summary>
        /// Cancels whatever was running, then runs <paramref name="work"/>.
        /// </summary>
        /// <returns>
        /// The result, and whether it is still wanted. <c>Superseded</c> means a newer request
        /// started while this one was in flight: the caller should publish nothing at all, since
        /// whatever is on screen now belongs to the newer request.
        /// </returns>
        public async Task<(bool Superseded, T? Result)> RunAsync<T>(Func<CancellationToken, Task<T>> work)
        {
            Cancel();

            var cancellation = new CancellationTokenSource();
            _current = cancellation;

            try
            {
                T result = await work(cancellation.Token).ConfigureAwait(false);
                return (cancellation.IsCancellationRequested, result);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Superseded or abandoned — not a failure, and nothing is waiting on it.
                return (true, default);
            }
            // Filtered, not caught wholesale: a cancellation this token did not ask for is a
            // failure wearing the same exception. HttpClient reports its own timeout as a
            // TaskCanceledException, and treating that as "superseded" made the caller publish
            // nothing at all — the weather skeleton spun forever on a slow network, with no retry
            // card, because nothing newer was ever coming to replace it.
            // Anything else is a real failure and belongs to the caller: only it knows whether
            // that means an error card, a retry, or a shrug.
        }

        public void Cancel()
        {
            _current?.Cancel();
            _current?.Dispose();
            _current = null;
        }

        public void Dispose() => Cancel();
    }
}
