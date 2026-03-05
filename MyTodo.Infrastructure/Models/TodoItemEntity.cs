//using MyTodo.Domain.Todo;


namespace MyTodo.Infrastructure.Models;

public class TodoItemEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoItemEntity() { }
    //public TodoItemEntity(TodoItem item)
    //{
    //    Id = item.Id.Value;
    //    Title = item.Title.Value;
    //    Done = item.IsCompleted.Value;
    //    CreatedAt = item.CreatedAt.Value;
    //}

    //public TodoItem ToTodoItem()
    //{
    //    return new TodoItem(
    //        Id: new TodoId(Id), 
    //        Title: new TodoTitle(Title), 
    //        IsCompleted: new TodoIsCompleted(Done),
    //        CreatedAt: new TodoCreatedAt(CreatedAt)
    //        );
    //}
}
