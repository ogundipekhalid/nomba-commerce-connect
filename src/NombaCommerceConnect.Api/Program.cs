using Microsoft.EntityFrameworkCore;
using NombaCommerceConnect.Application.Orders;
using NombaCommerceConnect.Application.Payments;
using NombaCommerceConnect.Infrastructure;
using NombaCommerceConnect.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Services -----------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Nomba Commerce Connect API",
        Version = "v1",
        Description = "A layered .NET connector wrapping Nomba's Checkout, Webhooks, Refunds, and split-payment APIs behind a clean REST surface, with a reference marketplace storefront."
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// Application use-case handlers - registered directly since this project has no
// mediator/pipeline library, keeping the dependency graph explicit and easy to read.
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<HandleNombaWebhookHandler>();
builder.Services.AddScoped<RefundOrderHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- Dev-only: auto-create the SQLite schema so the API runs with zero manual setup.
// A hackathon-appropriate stand-in for EF Core migrations. -----------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await NombaCommerceConnect.Api.DevDataSeeder.SeedIfEmptyAsync(db);
}

// --- Middleware pipeline --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
