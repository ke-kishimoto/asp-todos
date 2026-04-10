using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyTodo.Application.Queries.Categories;
using MyTodo.Application.Queries.Todos;
using MyTodo.Application.Repositories;
using MyTodo.Infrastructure.Data;
using MyTodo.Infrastructure.Queries;
using MyTodo.Infrastructure.Repositories;

namespace MyTodo.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ITodoRepository, EfTodoRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<ITodoQueryService, TodoQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();

        return services;
    }
}