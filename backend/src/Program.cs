using FluentValidation;
using FluentValidation.AspNetCore;


using Microsoft.OpenApi.Models;

using Parlamento.Application;
using Parlamento.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<IApplicationAssemblyMarker>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Parlamento API",
        Version = "v1",
        Description = "API for browsing Portuguese parliament proposals and collecting user votes."
    });
});

builder.Services.AddHealthChecks();


builder.Services.AddCors(options =>
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()));

var app = builder.Build();

// log environment
app.Logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(x => { x.SwaggerEndpoint("/swagger/v1/swagger.json", "Parlamento API v1"); });
}

// app.UseHttpsRedirection();

app.UseCors(builder =>
      {
          builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .WithMethods("GET", "PUT", "POST", "DELETE", "OPTIONS")
                .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));

      }
);

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/healthz");

app.Run();

public partial class Program { }
