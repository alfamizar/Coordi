using AutoMapper;
using JustCompute.Persistance.AutoMapper;

namespace JustCompute.DependencyInjectionExtensions;

public static class AutoMapperExtensions
{
    public static MauiAppBuilder ConfigureAutoMapper(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfile>();
        }));
        builder.Services.AddSingleton((serviceProvider) =>
        {
            var mapperConfiguration = serviceProvider.GetService<MapperConfiguration>();

            return mapperConfiguration.CreateMapper();
        });

        return builder;
    }
}