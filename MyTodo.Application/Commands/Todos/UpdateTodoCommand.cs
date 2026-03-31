using MyTodo.Application.Repositories;

namespace MyTodo.Application.Commands.Todos;

public record UpdateTodoCommand(int Id, string Title, bool Done);

public class UpdateTodoCommandHandler
{
    private readonly ITodoRepository _repo;

    public UpdateTodoCommandHandler(ITodoRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> HandleAsync(UpdateTodoCommand command)
    {
        return await _repo.UpdateAsync(command.Id, command.Title.Trim(), command.Done);
    }
}
