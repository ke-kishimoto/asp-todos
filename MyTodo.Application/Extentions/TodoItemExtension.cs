using MyTodo.Domain.Todo;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Application.Extentions;

public static class TodoItemExtension
{
    public static TodoItemEntity ToEntity(this TodoItem item)
    {
        return new TodoItemEntity
        {
            Id = item.Id.Value,
            Title = item.Title.Value,
            Done = item.IsCompleted.Value,
            CreatedAt = item.CreatedAt.Value
        };
    }

    public static TodoItem ToDomain(this TodoItemEntity entity)
    {
        return new TodoItem(
            Id: new TodoId(entity.Id),
            Title: new TodoTitle(entity.Title),
            IsCompleted: new TodoIsCompleted(entity.Done),
            CreatedAt: new TodoCreatedAt(entity.CreatedAt)
        );
    }
}