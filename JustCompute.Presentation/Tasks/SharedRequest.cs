namespace JustCompute.Presentation.Tasks
{
    /// <summary>
    /// One run of an operation at a time, shared by everyone who asks while it is out.
    ///
    /// The device fix is the case in point. The Locations screen, the place editor and the
    /// launch-time refresh can each ask for one, and two requests running side by side overwrote
    /// each other's state: the first to finish declared nothing in flight while the second was
    /// still waiting on the platform, and released anyone waiting on it early. Callers that
    /// arrive while a run is out now get that run; the first to arrive after it ends starts the
    /// next one.
    /// </summary>
    public sealed class SharedRequest<T>
    {
        private readonly Lock _gate = new();
        private Task<T>? _running;

        /// <summary>Whether a run is out right now.</summary>
        public bool IsRunning
        {
            get
            {
                lock (_gate)
                {
                    return _running is not null;
                }
            }
        }

        /// <summary>The run already out, or a new one started with <paramref name="start"/>.</summary>
        public Task<T> RunAsync(Func<Task<T>> start)
        {
            ArgumentNullException.ThrowIfNull(start);

            lock (_gate)
            {
                if (_running is { } running) return running;

                var started = RunToEndAsync(start);

                // Kept only while it is still running. A run that ended before this line has
                // already cleared the slot, and keeping it would hand its result, or its failure,
                // to every caller after it.
                if (!started.IsCompleted) _running = started;

                return started;
            }
        }

        private async Task<T> RunToEndAsync(Func<Task<T>> start)
        {
            try
            {
                return await start().ConfigureAwait(false);
            }
            finally
            {
                // Needs the gate, so it cannot clear the slot before RunAsync has filled it.
                lock (_gate)
                {
                    _running = null;
                }
            }
        }
    }
}
