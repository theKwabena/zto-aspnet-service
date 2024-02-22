using MigrateClient.Extensions;
using MigrateClient.Interfaces.Exchange;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.ConfigureConfidentialApp();

builder.Services.ConfigureEwsClient();
builder.Services.ConfigureBackgroundService();
builder.Services.ConfigureRedisService();
builder.Services.AddSignalR();



var app = builder.Build();

var ewss = app.Services.GetRequiredService<IExchangeWrapper>();
await ewss.Initialization;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.MapHub<>("/chatHub");

app.Run();
