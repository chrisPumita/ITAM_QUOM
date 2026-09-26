using ITAM.WebApp.ApiConnect;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApiConnectServiceCollectionExtensions
{
    public static IServiceCollection AddApiConnect(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiConnectSettings>(configuration.GetSection(ApiConnectSettings.SectionName));

        var settings = configuration.GetSection(ApiConnectSettings.SectionName).Get<ApiConnectSettings>()
            ?? throw new InvalidOperationException("Sección ApiConnect no configurada en appsettings.");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException("ApiConnect:BaseUrl es obligatorio.");

        services.AddHttpClient(ApiConnectDefaults.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 60 : settings.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddTransient<HttpRequestBuilder>();
        services.AddSingleton<ApiConnectFactory>();

        return services;
    }
}
