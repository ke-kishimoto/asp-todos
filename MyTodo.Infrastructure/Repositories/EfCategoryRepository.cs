using MyTodo.Application.Repositories;
using MyTodo.Infrastructure.Data;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Repositories;

public class EfCategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public EfCategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(string name)
    {
        _db.Categories.Add(new CategoryEntity
        {
            CategoryName = name,
            CreatedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(int id, string name)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return;
        entity.CategoryName = name;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return;
        _db.Categories.Remove(entity);
    }
}
