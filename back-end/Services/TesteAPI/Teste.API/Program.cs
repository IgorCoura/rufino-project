using Commom.API.Authentication;
using Commom.API.AuthorizationIds;
using Commom.Infra.Interface;
using Commom.MessageBroker;
using EasyNetQ.AutoSubscribe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Teste.API.Consumer;

// TESTE API

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMessageBrokerConfig(builder.Configuration, "teste_id");
builder.Services.AddScoped<BrandConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Services.GetRequiredService<AutoSubscriber>().SubscribeAsync(typeof(BrandConsumer).Assembly.GetTypes());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
