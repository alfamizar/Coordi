using System.Reflection;

namespace Compute.Core.Common.Events
{
    /// <summary>
    /// An event that does not keep its subscribers alive.
    ///
    /// A long-lived service raising an ordinary event holds a strong reference to every handler,
    /// so anything shorter-lived that subscribes — a transient view model, say — can never be
    /// collected. MAUI has a weak event manager for this, but it lives in the app; the domain
    /// services that need one live here, and cannot reference MAUI.
    ///
    /// Subscribe with a method, not a lambda. A lambda's target is a closure that nothing else
    /// references, so it is collected almost immediately and the handler silently stops firing.
    /// </summary>
    public sealed class WeakEvent<TArgs> where TArgs : EventArgs
    {
        private readonly List<Subscription> _subscriptions = [];
        private readonly Lock _gate = new();

        public void Add(EventHandler<TArgs>? handler)
        {
            if (handler is null) return;

            lock (_gate)
            {
                _subscriptions.Add(new Subscription(handler));
            }
        }

        public void Remove(EventHandler<TArgs>? handler)
        {
            if (handler is null) return;

            lock (_gate)
            {
                var index = _subscriptions.FindIndex(s => s.Matches(handler));
                if (index >= 0) _subscriptions.RemoveAt(index);
            }
        }

        public void Raise(object? sender, TArgs args)
        {
            // Snapshot under the lock and invoke outside it: a handler that subscribes or
            // unsubscribes in response must not deadlock, or change the list being walked.
            Subscription[] live;
            lock (_gate)
            {
                _subscriptions.RemoveAll(s => !s.IsAlive);
                live = [.. _subscriptions];
            }

            foreach (var subscription in live)
            {
                subscription.Invoke(sender, args);
            }
        }

        /// <summary>How many subscribers are still alive, for tests and nothing else.</summary>
        public int SubscriberCount
        {
            get
            {
                lock (_gate)
                {
                    return _subscriptions.Count(s => s.IsAlive);
                }
            }
        }

        private sealed class Subscription(EventHandler<TArgs> handler)
        {
            private readonly WeakReference<object>? _target =
                handler.Target is null ? null : new WeakReference<object>(handler.Target);

            private readonly MethodInfo _method = handler.Method;

            public bool IsAlive => _target is null || _target.TryGetTarget(out _);

            public bool Matches(EventHandler<TArgs> handler) =>
                handler.Method == _method
                && (_target is null
                    ? handler.Target is null
                    : _target.TryGetTarget(out var target) && ReferenceEquals(target, handler.Target));

            public void Invoke(object? sender, TArgs args)
            {
                object? target = null;
                if (_target is not null && !_target.TryGetTarget(out target)) return;

                try
                {
                    _method.Invoke(target, [sender, args]);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    // Surface the handler's own exception, not the reflection wrapper around it.
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
            }
        }
    }
}
