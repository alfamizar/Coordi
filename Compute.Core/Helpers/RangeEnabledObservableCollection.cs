using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Compute.Core.Helpers
{
    public class RangeEnabledObservableCollection<T> : ObservableCollection<T>
    {
        public RangeEnabledObservableCollection(IEnumerable<T> list) : base(list)
        {
        }

        public RangeEnabledObservableCollection()
        {
        }

        /// <summary>Appends every item, then announces the change once.</summary>
        public void InsertRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            foreach (var item in items)
                Items.Add(item);
            RaiseReset();
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            foreach (var item in items)
                Items.Remove(item);
            RaiseReset();
        }

        /// <summary>
        /// A collection reset, together with the property notifications the base class raises for
        /// its own Add and Remove.
        ///
        /// These bypass those methods to avoid an event per item, and used to skip the property
        /// notifications with them — so anything bound to Count or to an indexer never heard about
        /// a bulk change, and screens grew hand-maintained count properties to work around it.
        /// </summary>
        private void RaiseReset()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
