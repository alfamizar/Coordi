using JustCompute.Shared.Theming;

namespace JustCompute.Features.Settings
{
    public record ThemeOption(AppThemeId Theme, string Name)
    {
        public override string ToString() => Name;
    }
}
