using System.ComponentModel;

namespace JustCompute.Presentation.Collections
{
    /// <summary>What a group header binds to, whatever the group holds.</summary>
    public interface IGroupHeader : INotifyPropertyChanged
    {
        string Key { get; }

        /// <summary>What the header shows. The key unless the group was given a name of its own.</summary>
        string Title { get; }

        bool IsExpanded { get; set; }
    }

    public static class GroupingHelper
    {
        public class Group<TKey, TItem>(TKey key, IEnumerable<TItem> items, bool isExpanded = true)
            : RangeEnabledObservableCollection<TItem>(items), IGroupHeader
        {
            private bool _isExpanded = isExpanded;
            private string? _title;

            public TKey Key { get; private set; } = key;

            /// <summary>
            /// Raises a change, unlike the plain property it replaced: the header draws an open
            /// or closed marker from it, and that marker has to follow a tap.
            /// </summary>
            public bool IsExpanded
            {
                get => _isExpanded;
                set
                {
                    if (_isExpanded == value) return;
                    _isExpanded = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }

            public string Title
            {
                get => _title ?? KeyText;
                set
                {
                    if (_title == value) return;
                    _title = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Title)));
                }
            }

            string IGroupHeader.Key => KeyText;

            private string KeyText => Key as string ?? Key?.ToString() ?? string.Empty;
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
