using System.Collections.Specialized;
using System.ComponentModel;
using Compute.Core.Helpers;

namespace Compute.Core.Tests.Helpers
{
    /// <summary>
    /// The bulk operations bypass Add and Remove to avoid raising an event per item. That means
    /// they also have to raise the property notifications those methods would have — without
    /// them, anything bound to Count or to an indexer never hears about a bulk change.
    /// </summary>
    public class RangeEnabledObservableCollectionTests
    {
        private static (List<string> Properties, List<NotifyCollectionChangedAction> Actions) Record(
            RangeEnabledObservableCollection<int> collection)
        {
            List<string> properties = [];
            List<NotifyCollectionChangedAction> actions = [];

            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => properties.Add(e.PropertyName!);
            collection.CollectionChanged += (_, e) => actions.Add(e.Action);

            return (properties, actions);
        }

        [Fact]
        public void InsertRange_AnnouncesCountAndIndexer()
        {
            var collection = new RangeEnabledObservableCollection<int>();
            var (properties, actions) = Record(collection);

            collection.InsertRange([1, 2, 3]);

            Assert.Equal(3, collection.Count);
            Assert.Contains("Count", properties);
            Assert.Contains("Item[]", properties);
            Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
        }

        [Fact]
        public void RemoveRange_AnnouncesCountAndIndexer()
        {
            var collection = new RangeEnabledObservableCollection<int>([1, 2, 3]);
            var (properties, actions) = Record(collection);

            collection.RemoveRange([1, 2]);

            Assert.Equal([3], collection);
            Assert.Contains("Count", properties);
            Assert.Contains("Item[]", properties);
            Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
        }

        [Fact]
        public void BulkChange_RaisesOneCollectionEvent_NotOnePerItem()
        {
            var collection = new RangeEnabledObservableCollection<int>();
            var (_, actions) = Record(collection);

            collection.InsertRange(Enumerable.Range(0, 50));

            Assert.Single(actions);
        }
    }
}
