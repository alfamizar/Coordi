using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;

namespace JustCompute.Presentation.Helpers
{
    [ContentProperty(nameof(Key))]
    public class LocalizeExtension : IMarkupExtension
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        
        public string Key { get; set; } = string.Empty;

        public LocalizeExtension()
        {
            _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();
        }

        public object ProvideValue() => _localizer[Key].ToString();

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue();
    }
}
