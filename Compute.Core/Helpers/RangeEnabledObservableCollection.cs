using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

        public void InsertRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            foreach (var item in items)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
