namespace JustCompute.Shared.Design
{
    /// <summary>
    /// Marks the selected row of a list, for the ListRow style in Styles.xaml to draw.
    ///
    /// A CollectionView would mark it by itself, but inside a page that scrolls it is sized
    /// differently on each platform: Android grows it to fit every row, iOS gives it a window
    /// about one row high and scrolls the rest inside the card. A short list in a scrolling page
    /// is a stack of rows instead, and a stack has no selection of its own, so the page says
    /// which row is the selected one and the style's trigger on this draws it.
    /// </summary>
    public static class ListRow
    {
        public static readonly BindableProperty IsSelectedProperty = BindableProperty.CreateAttached(
            "IsSelected", typeof(bool), typeof(ListRow), false);

        public static bool GetIsSelected(BindableObject view) => (bool)view.GetValue(IsSelectedProperty);

        public static void SetIsSelected(BindableObject view, bool value) => view.SetValue(IsSelectedProperty, value);
    }
}
