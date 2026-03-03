using System.Runtime.InteropServices;

namespace MyTodo.Domain.Todo
{
    public record TodoId(int Value);
    public record TodoTitle(string Value);
    public record TodoIsCompleted(bool Value);
    public record TodoCreatedAt(DateTime Value);

    public record TodoItem(TodoId Id, TodoTitle Title, TodoIsCompleted IsCompleted, TodoCreatedAt CreatedAt);

    public record TodoItems(IEnumerable<TodoItem> Items)
    {
        public TodoItems AllCompleted()
        {
            return new TodoItems(Items.Select(item => item with { IsCompleted = new TodoIsCompleted(true) }));
        }
    }
}

