namespace MyTodo.Infrastructure.Models
{
    public class ItemEntity
    {
        public int Id { get; set; }
        public required string ItemCode { get; set; }
        public required string ItemName { get; set; }
        public int Price { get; set; } = 0;
    }
}
