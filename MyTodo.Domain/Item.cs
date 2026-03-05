
namespace MyTodo.Domain.Item
{
    public record ItemId(int Value);
    public record ItemCode(string Value);
    public record ItemName(string Value);
    public record ItemPrice(int Value);

    public record Item(ItemId Id, ItemName Name, ItemCode Code, ItemPrice Price);

}