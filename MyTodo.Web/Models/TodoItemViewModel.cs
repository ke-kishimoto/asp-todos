using MyTodo.Application.Queries.Todos;

namespace MyTodo.Web.Models;

public class TodoItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoItemViewModel() { }

    public TodoItemViewModel(TodoReadModel model)
    {
        Id = model.Id;
        Title = model.Title;
        Done = model.Done;
        CreatedAt = model.CreatedAt;
    }
}
