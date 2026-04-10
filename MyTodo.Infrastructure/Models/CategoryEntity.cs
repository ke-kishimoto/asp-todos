
namespace MyTodo.Infrastructure.Models;

public class CategoryEntity
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
