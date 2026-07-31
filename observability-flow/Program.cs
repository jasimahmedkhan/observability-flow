using ObservabilityFlow.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddCheckoutLogging(builder.Configuration);
builder.Services.AddCheckoutOpenTelemetry(builder.Configuration);
builder.Services.AddCheckoutApplication();
builder.Services.AddCheckoutOpenApi();

var app = builder.Build();

app.UseRequestTelemetry();
app.UseCheckoutOpenApi();
app.MapControllers();

app.Run();
