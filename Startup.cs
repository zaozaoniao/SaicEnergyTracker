using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Dataflow.SaicEnergyTracker
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }
        private ILogger _logger;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DataflowOption>(Configuration.GetSection(nameof(DataflowOption)));
            services.AddDbContextPool<CarModelContext>(options => 
                options.UseSqlServer(Configuration.GetConnectionString("SQL"),builder => builder.EnableRetryOnFailure(3))
                );
            services.AddScoped<LoggingActionFilter>();
            services.AddSingleton<EventHubClientWrapper>();
            services.AddSingleton<IMsgBatchSender, MsgBatchSender>();
            services.AddSwaggerGen(c =>
              {
                  c.SwaggerDoc("v1", new OpenApiInfo { Title = "SaicEnergyTracker API", Version = "v1" });
                  c.IncludeXmlComments($"{AppDomain.CurrentDomain.BaseDirectory}SaicEnergyTracker.xml");
              });

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IWebHostEnvironment env,ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/home/error");

            lifetime.ApplicationStopped.Register(() =>
            {
                var srv = app.ApplicationServices.GetService<IMsgBatchSender>();
                srv.CompleteAsync();
                _logger.LogInformation("Service Stoped.");
            });

            app.MapWhen(_=>_.Request.Path.StartsWithSegments("/api/v1/data-tracker"),app=>app.UseDataTrackerMiddleware());

            app.MapWhen(_ => _.Request.Path.Value == "/" || _.Request.Path.Value.Contains("favicon.ico"),
                appBuilder => appBuilder.Run(_ =>
                {
                    _.Response.StatusCode = StatusCodes.Status200OK;
                    _.Response.WriteAsync("ok");
                    return Task.CompletedTask;
                }));
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SaicEnergyTracker API V1");
            });
        }
    }
}
