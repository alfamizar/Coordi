using AutoMapper;
using JustCompute.Persistence.AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustCompute.DependencyInjectionExtensions;

public static class AutoMapperExtensions
{
    public static MauiAppBuilder ConfigureAutoMapper(this MauiAppBuilder builder)
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfile>();
        }, NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        builder.Services.AddSingleton(mapperConfig);
        builder.Services.AddSingleton(mapper);

        return builder;
    }
}
