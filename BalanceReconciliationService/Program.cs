using BalanceReconciliationService.Extensions;
using Serilog;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    config.WriteTo.Console();
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

app.Run();