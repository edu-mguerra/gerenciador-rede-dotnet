using GerenciadorRede.API.BackgroundServices;
using GerenciadorRede.API.Configurations;
using GerenciadorRede.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configurações desacopladas do container (Seu padrão limpo)
builder.AddSerilogLogging();
builder.AddDatabaseContext();

// Serviços tradicionais da API
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHostedService<GerenciadorRede.API.Services.TrafficSnifferService>();

// Motores de automação e segundo plano da rede
builder.Services.AddScoped<INetworkScannerService, NetworkScannerService>();
builder.Services.AddHostedService<NetworkWorker>();

var app = builder.Build();

// Configuração do Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();