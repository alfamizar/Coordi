namespace JustCompute.Features.Settings
{
    public record ThemeOption(AppTheme Theme, string Name)
    {
        public override string ToString() => Name;
    }
}
