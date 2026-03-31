using MyTodo.Application.Repositories;

namespace MyTodo.Application.Commands.Todos;

public record DeleteTodoCommand(int Id);

public class DeleteTodoCommandHandler
{
    private readonly ITodoRepository _repo;

    public DeleteTodoCommandHandler(ITodoRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> HandleAsync(DeleteTodoCommand command)
    {
        return await _repo.DeleteAsync(command.Id);
    }
}
