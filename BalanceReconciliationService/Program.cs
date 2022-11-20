using BalanceReconciliationService.Extensions;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => options.AddPolicy("allowAny", o =>
{
    o.AllowAnyOrigin();
    o.AllowAnyHeader();
    o.AllowAnyMethod();
}));

builder.Host.UseSerilog((context, config) =>
{
    config
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File(@"C:\Users\Matvey\source\repos\BalanceReconciliationService\Logs\logs.txt");
    //.WriteTo.Elasticsearch(
    //    new ElasticsearchSinkOptions(new Uri(context.Configuration["ElasticConfiguration:Uri"]))
    //    {
    //        IndexFormat = $"{context.Configuration["ApplicationName"]}-logs-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
    //        AutoRegisterTemplate = true,
    //        NumberOfShards = 2,
    //        NumberOfReplicas = 1
    //    })
    //.Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    //.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("allowAny");
app.UseHttpsRedirection();
app.ConfigureCustomExceptionMiddleware();

app.MapPost("/reconcileBalance", async Task<ReconciledOutputs> (MeasuredInputs measuredInputs) =>
{
    return await Task.Run(() =>
    {
        var dataPreparer = new MatrixDataPreparer(measuredInputs);
        var solver = new AccordSolver(dataPreparer);
        return solver.Solve();
    });
})
.RequireCors("allowAny");


app.MapPost("/getGlobalTest", async Task<double> (MeasuredInputs measuredInputs) =>
{
    return await Task.Run(() =>
    {
        var dataPreparer = new MatrixDataPreparer(measuredInputs);
        var globalTestCalculator = new GlobalTestCalculator(dataPreparer);
        return globalTestCalculator.GetGlobalTest();
    });
})
.RequireCors("allowAny");

Log.Information("Application starting up");

app.Run();