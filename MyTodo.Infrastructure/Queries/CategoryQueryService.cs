using Microsoft.EntityFrameworkCore;
using MyTodo.Application.Queries.Categories;
using MyTodo.Infrastructure.Data;

namespace MyTodo.Infrastructure.Queries;

public class CategoryQueryService : ICategoryQueryService
{
    private readonly AppDbContext _db;

    public CategoryQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryReadModel>> GetAllAsync()
    {
        return await _db.Categories
            .OrderBy(x => x.Id)
            .Select(x => new CategoryReadModel(x.Id, x.CategoryName, x.CreatedAt))
            .ToListAsync();
    }
}
