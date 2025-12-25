using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var orderServiceUrl = Environment.GetEnvironmentVariable("ORDER_SERVICE_URL") ?? "http://localhost:5001";
var paymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5002";

app.MapPost("/api/orders", async (HttpContext context, IHttpClientFactory clientFactory, CreateOrderDto dto) =>
{
    var userId = context.Request.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("X-User-Id header is required");

    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", userId);

    var response = await client.PostAsJsonAsync($"{orderServiceUrl}/api/orders", dto);
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.BadRequest(content);
});

app.MapGet("/api/orders", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var userId = context.Request.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("X-User-Id header is required");

    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", userId);

    var response = await client.GetAsync($"{orderServiceUrl}/api/orders");
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.BadRequest(content);
});

app.MapGet("/api/orders/{orderId}", async (Guid orderId, IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    var response = await client.GetAsync($"{orderServiceUrl}/api/orders/{orderId}");
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.NotFound(content);
});

app.MapPost("/api/accounts", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var userId = context.Request.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("X-User-Id header is required");

    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", userId);

    var response = await client.PostAsync($"{paymentServiceUrl}/api/accounts", null);
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.BadRequest(content);
});

app.MapPost("/api/accounts/deposit", async (HttpContext context, IHttpClientFactory clientFactory, DepositDto dto) =>
{
    var userId = context.Request.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("X-User-Id header is required");

    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", userId);

    var response = await client.PostAsJsonAsync($"{paymentServiceUrl}/api/accounts/deposit", dto);
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.BadRequest(content);
});

app.MapGet("/api/accounts/balance", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    var userId = context.Request.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("X-User-Id header is required");

    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", userId);

    var response = await client.GetAsync($"{paymentServiceUrl}/api/accounts/balance");
    var content = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(content))
        : Results.BadRequest(content);
});

app.Run();

public record CreateOrderDto(decimal Amount, string Description);
public record DepositDto(decimal Amount);
