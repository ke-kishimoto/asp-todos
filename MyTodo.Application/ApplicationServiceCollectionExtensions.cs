
using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;
using MyTodo.Application.Services;

namespace MyTodo.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IItemService, ItemService>();

        return services;
    }
}