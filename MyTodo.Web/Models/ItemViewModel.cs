using MyTodo.Domain.Item;

namespace MyTodo.Web.Models;

public class ItemViewModel
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int Price { get; set; } = 0;

    public ItemViewModel() { }

    public ItemViewModel(Item item)
    {
        Id = item.Id.Value;
        ItemCode = item.Code.Value;
        ItemName = item.Name.Value;
        Price = item.Price.Value;
    }

}