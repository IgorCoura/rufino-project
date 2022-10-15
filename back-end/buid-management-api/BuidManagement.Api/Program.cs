using BuidManagement.Api.Configuration;
using BuidManagement.Api.Filters;
using BuildManagement.Infra.Data.Context;
using BuildManagement.Service.Mappers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);

});

builder.Services.AddControllers(
opts =>
{
    opts.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    opts.Filters.Add(new ApplicationExceptionFilter());
});


builder.Services.ResolveDependencies(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


builder.Services.AddAuthConfig(builder.Configuration);

builder.Services.AddAutoMapper(typeof(EntityToModelProfile), typeof(ModelToEntityProfile));

ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-br");

builder.Services.AddSwaggerConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
