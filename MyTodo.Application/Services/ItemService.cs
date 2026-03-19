using MyTodo.Domain.Item;
using MyTodo.Infrastructure.Repositories;

namespace MyTodo.Application.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _repo;

    public ItemService(IItemRepository repo)
    {
        _repo = repo;
    }

    // 全件を取得し、Infrastructure 層の Entity → Domain モデルに変換する
    public async Task<IReadOnlyList<Item>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(ToItem).ToList();
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item == null ? null : ToItem(item);
    }

    public async Task<Item?> GetByItemCodeAsync(string itemCode)
    {
        var item = await _repo.GetByItemCodeAsync(itemCode);
        return item == null ? null : ToItem(item);
    }

    // 新規登録：トリム処理などのビジネスルールをここに集約する
    public async Task<Item> CreateAsync(string itemCode, string itemName, int price)
    {
        // ビジネスルールの例：入力値の前後空白を除去
        var entity = await _repo.AddAsync(itemCode.Trim(), itemName.Trim(), price);
        return ToItem(entity);
    }

    // Infrastructure Entity → Domain モデルの変換ヘルパー
    // コンストラクタの繰り返しを避けるために private メソッドとして抽出
    private static Item ToItem(MyTodo.Infrastructure.Models.ItemEntity e)
        => new Item(
            Id:    new ItemId(e.Id),
            Name:  new ItemName(e.ItemName),
            Code:  new ItemCode(e.ItemCode),
            Price: new ItemPrice(e.Price));
}