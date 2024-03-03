using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TodoApi;

 var builder = WebApplication.CreateBuilder(args);
// Add services
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"), new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
var app = builder.Build();
app.UseCors("AllowAll");


app.MapGet("/items", async (ToDoDbContext db) =>
    await db.Items.ToListAsync());

app.MapGet("/items/:{id}", async (int id,ToDoDbContext db) =>
{
    var item=await db.Items.FindAsync(id);
    if(item==null){
        return Results.NoContent();
    }
    return Results.Ok(item);
});

app.MapPost("/addTodo",async (Item todo,ToDoDbContext db) =>
{
    db.Items.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todoitems/{todo.Id}", todo);
});


// app.MapPut("/upDate/{id}", async (int id, Item item, [FromServices] ToDoDbContext context) =>
// {
//     var existingItem = await context.Items.FindAsync(id);
//     if (existingItem == null) return Results.NotFound();

//     existingItem.Name = item.Name;
//     existingItem.IsComplete = item.IsComplete;
//     await context.SaveChangesAsync();

//     return Results.NoContent();
// });
// תעתיקי את זה לפונקציה :
app.MapPut("/upDate/{id}", async (ToDoDbContext dbContext, HttpContext context, int id, Item updatedItem)=>
{
    if (updatedItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid task data");
        return;
    }

    var existingItem = await dbContext.Items.FindAsync(id);
    if (existingItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"Task with ID {id} not found");
        return;
    }

    if (updatedItem.Name != null)
    {
        existingItem.Name = updatedItem.Name;
    }

    existingItem.IsComplete = updatedItem.IsComplete;

    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsJsonAsync(existingItem);
});

app.MapDelete("/deleteTodo/{id}", async (int id,ToDoDbContext db) =>
{
    var todo= await db.Items.FindAsync(id) ;
    if (todo != null)
    {
        db.Items.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }
       return Results.NotFound();
});


app.UseCors("CorsPolicy");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger(options =>
{
    options.SerializeAsV2 = true;
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});
app.Run();