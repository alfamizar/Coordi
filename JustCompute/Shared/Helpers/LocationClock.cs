namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// A once-a-second tick carrying the wall-clock time at some place.
    ///
    /// Two screens keep a clock running for the location they show, and both grew their own
    /// start/stop/restart trio around a cancellation token. Pulling it out leaves the view models
    /// saying when the clock should run and for where, rather than how to run one.
    /// </summary>
    public sealed class LocationClock : IDisposable
    {
        private readonly Action<DateTime> _onTick;
        private CancellationTokenSource? _cancellation;

        public LocationClock(Action<DateTime> onTick) => _onTick = onTick;

        /// <summary>
        /// Restarts the clock for a place <paramref name="offsetHours"/> from UTC. Safe to call
        /// repeatedly: the previous tick is always cancelled first, so a location change cannot
        /// leave two clocks running against the same property.
        /// </summary>
        public void Start(double offsetHours = 0)
        {
            Stop();

            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;

            Application.Current?.Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (cancellation.IsCancellationRequested) return false;

                _onTick(DateTime.UtcNow.AddHours(offsetHours));
                return true;
            });
        }

        public void Stop()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        public void Dispose() => Stop();
    }
}
