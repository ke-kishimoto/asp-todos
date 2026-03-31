using MyTodo.Application.Repositories;
using MyTodo.Domain.Item;

namespace MyTodo.Application.Commands.Items;

public record CreateItemCommand(string ItemCode, string ItemName, int Price);

public class CreateItemCommandHandler
{
    private readonly IItemRepository _repo;

    public CreateItemCommandHandler(IItemRepository repo)
    {
        _repo = repo;
    }

    public async Task<Item> HandleAsync(CreateItemCommand command)
    {
        return await _repo.AddAsync(command.ItemCode.Trim(), command.ItemName.Trim(), command.Price);
    }
}
