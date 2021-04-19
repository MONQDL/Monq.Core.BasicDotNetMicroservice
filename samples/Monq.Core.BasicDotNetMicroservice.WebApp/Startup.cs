using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
using Monq.Core.BasicDotNetMicroservice.WebApp.ModelsExceptions;
using Newtonsoft.Json;

namespace Monq.Core.BasicDotNetMicroservice.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGlobalExceptionFilter()
                .AddExceptionHandler<ResponseException>((ex) => new ObjectResult(JsonConvert.DeserializeObject(ex.ResponseData))
                {
                    StatusCode = (int)ex.StatusCode
                })
                .AddDefaultExceptionHandlers();

            services.ConfigureSMAuthentication(Configuration);

            // Add framework services.
            services.AddControllers(opt => opt.Filters.Add(typeof(GlobalExceptionFilter)))
                .AddMetrics();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseTraceEventId();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseLogUser();
            app.UseEndpoints(e => e.MapControllers());
        }
    }
}
