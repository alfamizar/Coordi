namespace Compute.Core.Helpers
{
    public static class GroupingHelper
    {
        public class Group<TKey, TItem> : RangeEnabledObservableCollection<TItem>
        {
            public TKey Key { get; private set; }
            public bool IsExpanded { get; set; }

            public Group(TKey key, IEnumerable<TItem> items, bool isExpanded = true) : base(items)
            {
                Key = key;
                IsExpanded = isExpanded;
            }
        }

        public static IEnumerable<Group<TKey, TItem>> GetGroupedData<TItem, TKey>(IEnumerable<TItem> data,
            Func<TItem, TKey> groupKeySelector)
        {
            var groupedData = data
                .GroupBy(groupKeySelector)
                .Select(itemGroup => new Group<TKey, TItem>(itemGroup.Key, itemGroup))
                .ToList();

            return groupedData;
        }
    }
}