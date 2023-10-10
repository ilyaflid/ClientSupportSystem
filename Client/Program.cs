using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using ClientSupport.Common.DTO;
using ClientSupport.Common.ClientModels;
using System.ComponentModel.Design.Serialization;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: $"[{{Timestamp}} {{Level:u3}}] {{SourceContext}} | {{Message:l}} {{NewLine}}{{Exception}}") //{{Properties}}b
    .CreateLogger();

var logger = Log.Logger.ForContext("SourceContext", "Application");
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:44375");

CreateSessionResponse? sessionResponse;

try
{
    logger.Information("Client application started");

    using (var response = await httpClient.PostAsJsonAsync("/api/session/create", new { }))
    {
        if (!response.IsSuccessStatusCode)
            throw new Exception("The remote server responded with an error");

        sessionResponse = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        if (sessionResponse == null)
            throw new Exception("The remote server responded with unrecognized message");
    }
    
    if (sessionResponse.Code == CreateSessionResponseCode.TooBusy)
        logger.Information("There are too many connections at the time. Please try again later");

    if (sessionResponse.Code == CreateSessionResponseCode.Created)
    {
        logger.Information($"Session created: { sessionResponse.SessionId }");

        while (true)
        {
            Thread.Sleep(1000);
            using (var response = await httpClient.PutAsJsonAsync("/api/session/ping", new ProlongateSessionRequest() { SessionId = new Guid(sessionResponse.SessionId) }))
            {
                if (!response.IsSuccessStatusCode)
                    throw new Exception("The remote server responded with an error");
            }
        }
    }
}
catch (TaskCanceledException)
{
    logger.Information("Processing cancelled. Terminating.");
}
catch (Exception e)
{
    logger.Fatal(e, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
