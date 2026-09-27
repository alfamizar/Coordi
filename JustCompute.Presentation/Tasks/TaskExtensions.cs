namespace JustCompute.Presentation.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Lets a task run without anyone awaiting it, while still hearing about it when it fails.
        ///
        /// A bare <c>_ = SomethingAsync();</c> discards the task, and with it any exception: the
        /// failure surfaces later as an unobserved task exception, if at all, with nothing to say
        /// which screen it came from. Cancellation is not reported, because a load abandoned for
        /// a newer one is not a fault.
        /// </summary>
        public static void Forget(this Task task, Action<Exception> onFault)
        {
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(onFault);

            if (task.IsCompleted)
            {
                Observe(task, onFault);
                return;
            }

            task.ContinueWith(
                completed => Observe(completed, onFault),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static void Observe(Task task, Action<Exception> onFault)
        {
            if (!task.IsFaulted || task.Exception is null)
            {
                return;
            }

            // One cause is the usual case, and handing over the AggregateException around it
            // would bury the message a reader needs under one that says nothing.
            var exception = task.Exception.InnerExceptions.Count == 1
                ? task.Exception.InnerExceptions[0]
                : task.Exception;

            onFault(exception);
        }
    }
}
