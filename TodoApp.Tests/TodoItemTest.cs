
using MyTodo.Domain.Todo;

namespace TodoApp.Test.Domain;
public class TodoItemTest
{
    [Fact]
    public void AllCompletedTest()
    {
        var items = new TodoItems(
            [
                new TodoItem(new TodoId(1), new TodoTitle("Task 1"), new TodoIsCompleted(false), new TodoCreatedAt(DateTime.UtcNow)),
                new TodoItem(new TodoId(2), new TodoTitle("Task 2"), new TodoIsCompleted(false), new TodoCreatedAt(DateTime.UtcNow))
            ]
        );
        var completedItems = items.AllCompleted();
        Assert.All(completedItems.Items, item => Assert.True(item.IsCompleted.Value));
    }
}
