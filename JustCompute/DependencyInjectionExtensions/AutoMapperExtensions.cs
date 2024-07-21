using AutoMapper;
using JustCompute.Persistance.AutoMapper;

namespace JustCompute.DependencyInjectionExtensions;

public static class AutoMapperExtensions
{
    public static MauiAppBuilder ConfigureAutoMapper(this MauiAppBuilder builder)
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfile>();
        });

        IMapper mapper = mapperConfig.CreateMapper();

        builder.Services.AddSingleton(mapperConfig);
        builder.Services.AddSingleton(mapper);

        return builder;
    }
}