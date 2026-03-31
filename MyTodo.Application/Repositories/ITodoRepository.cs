using MyTodo.Domain.Todo;

namespace MyTodo.Application.Repositories;

public interface ITodoRepository
{
    Task<TodoItem> AddAsync(string title);
    Task<bool> UpdateAsync(int id, string title, bool done);
    Task<bool> DeleteAsync(int id);
}
