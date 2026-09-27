namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// <c>{localization:Localize Key}</c> in XAML. Reads the resource directly, like the value
    /// converters: XAML creates this itself, so it has no constructor to be handed a localizer
    /// through, and reaching into the service container for one made every page depend on the
    /// container having been built first.
    /// </summary>
    [ContentProperty(nameof(Key))]
    [AcceptEmptyServiceProvider]
    public class LocalizeExtension : IMarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public object ProvideValue() => Strings.Get(Key);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue();
    }
}
