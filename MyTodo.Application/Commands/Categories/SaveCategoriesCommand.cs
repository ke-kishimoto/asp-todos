using MyTodo.Application.Repositories;

namespace MyTodo.Application.Commands.Categories;

public record CategoryChange(int Id, string Name);

public record SaveCategoriesCommand(
    IReadOnlyList<string> Added,
    IReadOnlyList<CategoryChange> Updated,
    IReadOnlyList<int> Deleted);

public class SaveCategoriesCommandHandler
{
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public SaveCategoriesCommandHandler(
        ICategoryRepository repo,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(SaveCategoriesCommand command)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var id in command.Deleted)
                await _repo.DeleteAsync(id);

            foreach (var change in command.Updated)
                await _repo.UpdateAsync(change.Id, change.Name.Trim());

            foreach (var name in command.Added)
                await _repo.AddAsync(name.Trim());

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
