using System.Runtime.CompilerServices;
using Compute.Core.Common.Events;

namespace Compute.Core.Tests.Common
{
    public class WeakEventTests
    {
        private sealed class Listener
        {
            public int Calls { get; private set; }

            public void OnRaised(object? sender, EventArgs e) => Calls++;
        }

        [Fact]
        public void ALiveSubscriberHearsTheEvent()
        {
            var weak = new WeakEvent<EventArgs>();
            var listener = new Listener();
            weak.Add(listener.OnRaised);

            weak.Raise(this, EventArgs.Empty);
            weak.Raise(this, EventArgs.Empty);

            Assert.Equal(2, listener.Calls);
        }

        [Fact]
        public void ARemovedSubscriberHearsNothing()
        {
            var weak = new WeakEvent<EventArgs>();
            var listener = new Listener();
            weak.Add(listener.OnRaised);
            weak.Remove(listener.OnRaised);

            weak.Raise(this, EventArgs.Empty);

            Assert.Equal(0, listener.Calls);
        }

        [Fact]
        public void TheEventDoesNotKeepItsSubscriberAlive()
        {
            // The whole point: a long-lived service must not pin a short-lived subscriber.
            var weak = new WeakEvent<EventArgs>();
            var reference = SubscribeAndDrop(weak);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(reference.IsAlive);
            weak.Raise(this, EventArgs.Empty);   // must not throw on the dead entry
            Assert.Equal(0, weak.SubscriberCount);
        }

        [Fact]
        public void AHandlersOwnExceptionSurfacesUnwrapped()
        {
            var weak = new WeakEvent<EventArgs>();
            var thrower = new Thrower();
            weak.Add(thrower.OnRaised);

            Assert.Throws<InvalidOperationException>(() => weak.Raise(this, EventArgs.Empty));
        }

        private sealed class Thrower
        {
            public void OnRaised(object? sender, EventArgs e) => throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeAndDrop(WeakEvent<EventArgs> weak)
        {
            var listener = new Listener();
            weak.Add(listener.OnRaised);
            return new WeakReference(listener);
        }
    }
}
