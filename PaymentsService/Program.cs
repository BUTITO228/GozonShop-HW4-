using Microsoft.EntityFrameworkCore;
using PaymentsService.BackgroundServices;
using PaymentsService.Data;
using PaymentsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=payments_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();
builder.Services.AddHostedService<PaymentRequestConsumer>();
builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PaymentsDbContext>();
        context.Database.EnsureCreated();   
        Console.WriteLine("Database created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating database: {ex.Message}");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
