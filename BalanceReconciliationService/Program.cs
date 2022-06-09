var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/reconcileBalance", async (MeasuredInputs measuredInputs) =>
{
    await Task.Run(() =>
    {
        var solver = new AccordSolver(measuredInputs);
        return solver.Solve();        
    });
})
.WithName("ReconcileBalance").RequireCors("allowAny");

app.Run();
