namespace JK.Tools.Monitoring.AspNetCore
{
    public class ParameterMonitoringOptions
    {
        public bool TraceRouteData { get; set; } = true;

        public bool TraceQueryString { get; set; } = true;

        public bool TraceRequestHeaders { get; set; } = false;

        public bool TraceRequestCookies { get; set; } = false;

        public bool TraceRequestBody { get; set; } = false;

        public bool TraceResponseBody { get; set; } = false;

        public bool TraceResponseHeaders { get; set; } = false;

        public bool TraceResponseStatusCode { get; set; } = true;
    }
}
