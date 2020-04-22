namespace JK.Tools.Monitoring.TestApp
{
    using JK.Tools.Monitoring.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Models;

    public class Startup
    {
        private const string AppName = "Monitoring tools test app";
        private const string AppVersion = "v1";
        private static readonly string SwaggerEndpoint = $"/swagger/{AppVersion}/swagger.json";

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc(
                        AppVersion,
                        new OpenApiInfo
                        {
                            Title = AppName,
                            Version = AppVersion,
                        });
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseParameterMonitoring(options =>
            {
                options.TraceRouteData = true;
                options.TraceQueryString = true;
                options.TraceRequestHeaders = true;
                options.TraceRequestCookies = true;
                options.TraceRequestBody = true;
                options.TraceResponseBody = true;
                options.TraceResponseHeaders = true;
                options.TraceResponseStatusCode = true;
            });

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint(SwaggerEndpoint, AppName);
            });
        }
    }
}
