using Polly;

namespace JustCompute.DependencyInjectionExtensions;

public static class PollyExtensions
{
    public static MauiAppBuilder ConfigurePolly(this MauiAppBuilder builder)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        builder.Services.AddSingleton(retryPolicy);

        return builder;
    }
}