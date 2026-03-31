using Microsoft.EntityFrameworkCore;
using MyTodo.Application.Queries.Todos;
using MyTodo.Infrastructure.Data;

namespace MyTodo.Infrastructure.Queries;

public class TodoQueryService : ITodoQueryService
{
    private readonly AppDbContext _db;

    public TodoQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoReadModel>> GetAllAsync()
    {
        return await _db.Todos
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoReadModel(x.Id, x.Title, x.Done, x.CreatedAt))
            .ToListAsync();
    }

    public async Task<TodoReadModel?> GetByIdAsync(int id)
    {
        var entity = await _db.Todos.FindAsync(id);
        return entity is null
            ? null
            : new TodoReadModel(entity.Id, entity.Title, entity.Done, entity.CreatedAt);
    }

    public async Task<IReadOnlyList<TodoReadModel>> SearchAsync(string keyword)
    {
        return await _db.Todos
            .Where(x => x.Title.Contains(keyword))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TodoReadModel(x.Id, x.Title, x.Done, x.CreatedAt))
            .ToListAsync();
    }
}
