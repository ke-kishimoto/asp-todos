using MyTodo.Domain.Todo;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Mappings;

internal static class TodoItemMapping
{
    internal static TodoItem ToDomain(TodoItemEntity entity)
        => new TodoItem(
            Id: new TodoId(entity.Id),
            Title: new TodoTitle(entity.Title),
            IsCompleted: new TodoIsCompleted(entity.Done),
            CreatedAt: new TodoCreatedAt(entity.CreatedAt));
}
