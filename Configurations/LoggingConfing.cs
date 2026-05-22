using Serilog;

namespace GerenciadorRede.API.Configurations
{
    public static class LoggingConfing
    {
        public static void AddSerilogLogging(this WebApplicationBuilder builder)
        {

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();


            builder.Host.UseSerilog();



        }
    }
}
