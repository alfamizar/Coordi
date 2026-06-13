using Microsoft.Maui.Handlers;

namespace JustCompute.Handlers.ExtendedSearchBar;

public partial class SearchBarExHandler 
{
    partial void SuppressPlatformCancelButton();

    public static readonly IPropertyMapper<ISearchBar, SearchBarHandler> SearchBarMapper =
        new PropertyMapper<ISearchBar, SearchBarHandler>(Mapper)
        {
            ["IconColor"] = MapIconColor,
        };

    public SearchBarExHandler() : base(SearchBarMapper)
    {
    }

    public SearchBarExHandler(IPropertyMapper mapper) : base(mapper ?? SearchBarMapper)
    {
    }

    public override void UpdateValue(string propertyName)
    {
        base.UpdateValue(propertyName);
        SuppressPlatformCancelButton();

        if (propertyName == SearchBar.TextColorProperty.PropertyName)
        {
            SetIconColor(GetTextColor());
        }
    }

    public static void MapIconColor(ISearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler is SearchBarExHandler searchBarHandler)
            searchBarHandler.SetIconColor(searchBarHandler.GetTextColor());
    }
}
