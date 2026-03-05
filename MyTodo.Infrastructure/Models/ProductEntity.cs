namespace MyTodo.Infrastructure.Models;

public class ProductEntity
{
    public int Id { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public int Price { get; set; } = 0;
}