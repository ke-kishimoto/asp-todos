using MyTodo.Application.Queries.Items;

namespace MyTodo.Web.Models;

public class ItemViewModel
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int Price { get; set; } = 0;

    public ItemViewModel() { }

    public ItemViewModel(ItemReadModel model)
    {
        Id = model.Id;
        ItemCode = model.ItemCode;
        ItemName = model.ItemName;
        Price = model.Price;
    }
}
