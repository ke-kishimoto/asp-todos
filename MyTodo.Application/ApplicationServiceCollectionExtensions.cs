using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyTodo.Application.Commands.Categories;
using MyTodo.Application.Commands.Todos;

namespace MyTodo.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<CreateTodoCommandHandler>();
        services.AddScoped<UpdateTodoCommandHandler>();
        services.AddScoped<DeleteTodoCommandHandler>();
        services.AddScoped<SaveCategoriesCommandHandler>();

        return services;
    }
}