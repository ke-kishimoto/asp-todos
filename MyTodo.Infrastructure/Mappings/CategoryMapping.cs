using MyTodo.Domain.Category;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Mappings;

internal static class CategoryMapping
{
    internal static Category ToDomain(CategoryEntity entity)
        => new Category(
            Id: new CategoryId(entity.Id),
            Name: new CategoryName(entity.CategoryName),
            CreatedAt: new CategoryCreatedAt(entity.CreatedAt));
}
