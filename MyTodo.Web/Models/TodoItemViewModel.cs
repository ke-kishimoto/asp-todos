using MyTodo.Domain.Todo;


namespace MyTodo.Web.Models;

public class TodoItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TodoItemViewModel() { }
 
   public TodoItemViewModel(TodoItem item)
    {
       Id = item.Id.Value;
       Title = item.Title.Value;
       Done = item.IsCompleted.Value;
       CreatedAt = item.CreatedAt.Value;
    }

}
