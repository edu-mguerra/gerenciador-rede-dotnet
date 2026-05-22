using GerenciadorRede.API.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore; // <-- ESSENCIAL: Ativa o UseMySql e ServerVersion
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GerenciadorRede.API.Configurations
{
    public static class DatabaseConfiguration
    {
        public static void AddDatabaseContext(this WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada no arquivo de configuração.");
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        }
    }
}