using Microsoft.EntityFrameworkCore;
using MyTodo.Application.Repositories;
using MyTodo.Domain.Todo;
using MyTodo.Infrastructure.Data;
using MyTodo.Infrastructure.Mappings;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Repositories;

public class EfTodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;

    public EfTodoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TodoItem> AddAsync(string title)
    {
        var entity = new TodoItemEntity
        {
            Title = title,
            Done = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Todos.Add(entity);
        await _db.SaveChangesAsync();
        return TodoItemMapping.ToDomain(entity);
    }

    public async Task<bool> UpdateAsync(int id, string title, bool done)
    {
        var entity = await _db.Todos.FindAsync(id);
        if (entity is null) return false;

        entity.Title = title;
        entity.Done = done;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Todos.FindAsync(id);
        if (entity is null) return false;

        _db.Todos.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
