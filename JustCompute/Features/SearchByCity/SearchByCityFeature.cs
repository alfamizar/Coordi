namespace JustCompute.Features.SearchByCity;

public static class SearchByCityFeature
{
    public static MauiAppBuilder AddSearchByCityFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SearchByCityPage>();
        builder.Services.AddSingleton<SearchByCityViewModel>();
        return builder;
    }
}
