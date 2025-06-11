using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

// Add a depencency injection container
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

// Use a rewriter to redirect tasks to todos
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

// Use a custom middleware to log requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} Started]");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} Completed]");
});

var todos = new List<Todo>();

// Get all todos
app.MapGet("/todos", (ITaskService service) => service.GetTodos());

// Get a specific todo by ID
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});

// Create a new todo
app.MapPost("/todos", (Todo task, ITaskService service) =>
{
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past."]);
    }

    if (taskArgument.IsComplete)
    {
        errors.Add(nameof(Todo.IsComplete), ["Cannot add a completed task."]);
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

// Delete a todo by ID
app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsComplete);

interface ITaskService
{
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);
    Todo AddTodo(Todo todo);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = new();

    public Todo AddTodo(Todo todo)
    {
        _todos.Add(todo);
        return todo;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(t => t.Id == id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => t.Id == id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }
}