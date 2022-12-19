using BalanceReconciliationService.Extensions;
using Microsoft.AspNetCore.Mvc;
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
        var dataPreparer = new MatrixDataPreparer(measuredInputs.FlowsData);
        var solver = new AccordSolver(dataPreparer, measuredInputs.ConstraintsType);
        return solver.Solve();
    });
})
.RequireCors("allowAny");


app.MapPost("/getGlobalTest", async Task<double> (MeasuredInputs measuredInputs) =>
{
    return await Task.Run(() =>
    {
        var dataPreparer = new MatrixDataPreparer(measuredInputs.FlowsData);
        var globalTestCalculator = new GlobalTestCalculator(dataPreparer);
        return globalTestCalculator.GetSourceSystemGlobalTest();
    });
})
.RequireCors("allowAny");

app.MapPost("/detectGlobalErrors", async Task<IEnumerable<GrossErrorDetectionResult>> (GedInputs gedInputs) =>
{
    return await Task.Run(() =>
    {
        var dataPreparer = new MatrixDataPreparer(gedInputs.FlowsData);
        var gedDataPreparer = new GedDataPreparer(gedInputs);
        var globalTestCalculator = new GlobalTestCalculator(dataPreparer);
        var grossErrorDetectionService = new GrossErrorDetectionService(globalTestCalculator);
        var constraintsType = gedInputs.ConstraintsType;
        var branching = gedDataPreparer.Branching;
        var maxTreeHeight = gedDataPreparer.MaxTreeHeight;
        var maxSolutionsCount = gedDataPreparer.MaxSolutionsCount;
        var errors = gedInputs.Errors;

        var FlowsWithErrors = grossErrorDetectionService.GrossErrorDetectionByTree(dataPreparer.FlowsData, branching, maxTreeHeight, maxSolutionsCount, errors);

        return FlowsWithErrors;
    });
})
.RequireCors("allowAny");

app.MapPost("/detectAndReconcileGlobalErrors", async Task<IEnumerable<GrossErrorDetectionAndReconcilationResult>> (GedInputs gedInputs) =>
{
    return await Task.Run(() =>
    {
        var dataPreparer = new MatrixDataPreparer(gedInputs.FlowsData);
        var gedDataPreparer = new GedDataPreparer(gedInputs);
        var globalTestCalculator = new GlobalTestCalculator(dataPreparer);
        var grossErrorDetectionService = new GrossErrorDetectionService(globalTestCalculator);
        var constraintsType = gedInputs.ConstraintsType;
        var branching = gedDataPreparer.Branching;
        var maxTreeHeight = gedDataPreparer.MaxTreeHeight;
        var maxSolutionsCount = gedDataPreparer.MaxSolutionsCount;
        var errors = gedInputs.Errors;

        var reconciledFlowsWithErrors = grossErrorDetectionService.GrossErrorDetectionAndReconcilationByTree(dataPreparer.FlowsData, constraintsType, branching, maxTreeHeight, maxSolutionsCount, errors);

        return reconciledFlowsWithErrors;
    });
})
.RequireCors("allowAny");

Log.Information("Application starting up");

app.Run();