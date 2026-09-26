using ITAM.Application.Services.Assets;
using ITAM.Application.Services.Assignments;
using ITAM.Application.Services.Catalog;
using ITAM.Application.Services.Company;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Domain.Interfaces.Services.Company;
using Microsoft.Extensions.DependencyInjection;

namespace ITAM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetAssignmentService, AssetAssignmentService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ISupplierService, SupplierService>();

        return services;
    }
}
