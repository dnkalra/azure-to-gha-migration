using SampleApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IOrderService, OrderService>();

var app = builder.Build();

app.MapGet("/orders/{id:int}", (int id, IOrderService service) =>
{
    var order = service.GetOrder(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.Run();
