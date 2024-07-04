using JustCompute.Handlers.ExtendedSearchBar;
using JustCompute.Presentation.ExtendedControls;

namespace JustCompute.DependencyInjectionExtensions
{
    public static class MauiHandlersExtensions
    {
        public static MauiAppBuilder ConfigureMauiHandlers(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers(collection =>
            {
                collection.AddHandler<SearchBar, SearchBarExHandler>();
#if __ANDROID__
                collection.AddHandler(typeof(CustomSwitch), typeof(JustCompute.Platforms.Android.UI.Handlers.CustomSwitchHandler));
#elif WINDOWS
                collection.AddHandler(typeof(CustomSwitch), typeof(JustCompute.Platforms.Windows.UI.Handlers.CustomSwitchHandler));
#endif
            });
            return builder;
        }
    }
}