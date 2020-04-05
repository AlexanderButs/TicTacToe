using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NJsonSchema;

using NLog;

using NSwag.AspNetCore;

using TicTacToe.Repositories;

namespace TicTacToe
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public const string SwaggerRoute = "/swagger";

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(config =>
            {
                config.AddDebug();
                config.AddConsole();
            });

            // Add framework services.
            services.AddSwaggerDocument();
            
            services.AddControllers();

            services.AddSingleton<IGameRepository, GameRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            { 
                endpoints.MapControllers();
            });

            app.UseSwaggerUi3(settings =>
            {
                settings.Path = SwaggerRoute;
            });
        }
    }
}
