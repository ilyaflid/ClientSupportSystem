using ClientSupportService;
using ClientSupportService.Host;
using ClientSupportService.Interfaces;
using Serilog;
using Serilog.Events;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: $"[{{Timestamp}} {{Level:u3}}] {{SourceContext}} | {{Message:l}} {{NewLine}}{{Exception}}")
    .WriteTo.File("log.txt", outputTemplate: $"[{{Timestamp}} {{Level:u3}}] {{SourceContext}} | {{Message:l}} {{NewLine}}{{Exception}}")
    .CreateLogger();

var logger = Log.Logger.ForContext("SourceContext", "ApplicationBootstrap");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ISessionManager, SessionManager>();
builder.Services.AddSingleton<IClientSupportServiceConfiguration>( 
        new ConfigurationSettingsProvider(builder.Configuration));
builder.Services.AddSingleton<ISessionStorage, SessionInMemoryStorage>();
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton(logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
