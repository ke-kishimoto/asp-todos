using MyTodo.Domain.Item;
using MyTodo.Infrastructure.Models;

namespace MyTodo.Infrastructure.Mappings;

internal static class ItemMapping
{
    internal static Item ToDomain(ItemEntity entity)
        => new Item(
            Id: new ItemId(entity.Id),
            Name: new ItemName(entity.ItemName),
            Code: new ItemCode(entity.ItemCode),
            Price: new ItemPrice(entity.Price));
}
