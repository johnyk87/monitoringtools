namespace JK.Tools.Monitoring.AspNetCore
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Internal;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseParameterMonitoring(
            this IApplicationBuilder app)
        {
            return app.UseParameterMonitoring(null);
        }

        public static IApplicationBuilder UseParameterMonitoring(
            this IApplicationBuilder app,
            Action<ParameterMonitoringOptions> setupAction)
        {
            var options = new ParameterMonitoringOptions();

            setupAction?.Invoke(options);

            /*
             * In order to be able to obtain the route information before the MVC middleware executes,
             * we need to enable endpoint routing, so that it acts as the global routing mechanism and
             * fills the necessary data for route parameters tracing.
             */
            if (options.TraceRouteData)
            {
                app.UseEndpointRouting();
            }

            return app.UseMiddleware<ParameterMonitoringMiddleware>(options);
        }
    }
}
