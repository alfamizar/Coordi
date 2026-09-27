using JustCompute.Presentation.Tasks;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// Where failures nobody is waiting on get written down.
    ///
    /// Console rather than Debug: Debug.WriteLine is compiled out of Release, so a failure in a
    /// shipped build left no trace at all — which is how a spinner turning forever once went
    /// unexplained. Page lifecycle calls and every fire-and-forget load report through here, so
    /// there is one place to change if this ever needs to become real logging.
    /// </summary>
    public static class Diagnostics
    {
        public static void Report(string source, Exception exception) =>
            Console.WriteLine($"[{source}] unhandled exception: {exception}");

        /// <summary>Runs <paramref name="task"/> unawaited, reporting a failure under <paramref name="source"/>.</summary>
        public static void Forget(this Task task, string source) =>
            task.Forget(exception => Report(source, exception));
    }
}
