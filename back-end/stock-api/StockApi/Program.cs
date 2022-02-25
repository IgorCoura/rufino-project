using Microsoft.EntityFrameworkCore;
using StockApi.Domain.Interfaces;
using StockApi.Infra.Data.Context;
using StockApi.Infra.Data.Repository;
using StockApi.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
    builder =>
    {
        builder.AllowAnyOrigin();
        builder.AllowAnyHeader();
        builder.AllowAnyMethod();
    });
});

builder.Services.AddScoped<IServiceWorker,WorkerService>();
builder.Services.AddScoped<IRepositoryWorker,WorkerRepository>();
builder.Services.AddScoped<IServiceProduct, ProductService>();
builder.Services.AddScoped<IRepositoryProduct, ProductRepository>();
builder.Services.AddScoped<IServiceProductTransaction, ProductTransactionService>();
builder.Services.AddScoped<IRepositoryProductTransaction, ProductTransactionRepository>();

builder.Services.AddDbContext<StockApiContext>(options => 
    options.UseNpgsql(builder.Configuration["ConnectionString"],
    npgsqlOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
    }
  )
);



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
