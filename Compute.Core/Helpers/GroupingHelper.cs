namespace Compute.Core.Helpers
{
    public interface IGroupHeader
    {
        string Key { get; }
        bool IsExpanded { get; set; }
    }

    public static class GroupingHelper
    {
        public class Group<TKey, TItem>(TKey key, IEnumerable<TItem> items, bool isExpanded = true)
            : RangeEnabledObservableCollection<TItem>(items), IGroupHeader
        {
            public TKey Key { get; private set; } = key;
            public bool IsExpanded { get; set; } = isExpanded;

            string IGroupHeader.Key => Key as string ?? Key?.ToString() ?? string.Empty;
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