namespace JK.Tools.Monitoring.AspNetCore
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ParameterMonitoringMiddleware
    {
        private const string RequestContext = "request";
        private const string ResponseContext = "response";
        private const string ApplicationJsonMimeType = "application/json";
        private const int DefaultStreamReaderBufferSize = 1024;
        private readonly RequestDelegate next;
        private readonly ParameterMonitoringOptions options;

        public ParameterMonitoringMiddleware(RequestDelegate next)
            : this(next, null)
        {
        }

        public ParameterMonitoringMiddleware(
            RequestDelegate next,
            ParameterMonitoringOptions options)
        {
            this.next = next;
            this.options = options ?? new ParameterMonitoringOptions();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (this.options.TraceRouteData)
            {
                TraceRouteData(context.GetRouteData(), RequestContext);
            }

            if (this.options.TraceQueryString)
            {
                TraceQueryString(context.Request.Query, RequestContext);
            }

            if (this.options.TraceRequestHeaders)
            {
                TraceHeaders(context.Request.Headers, RequestContext);
            }

            if (this.options.TraceRequestCookies)
            {
                TraceCookies(context.Request.Cookies, RequestContext);
            }

            if (this.options.TraceRequestBody)
            {
                await TraceBodyAsync(context.Request, RequestContext).ConfigureAwait(false);
            }

            if (this.options.TraceResponseBody)
            {
                await TraceBodyAsync(context.Response, ResponseContext, () => this.next(context)).ConfigureAwait(false);
            }
            else
            {
                await this.next(context).ConfigureAwait(false);
            }

            if (this.options.TraceResponseHeaders)
            {
                TraceHeaders(context.Response.Headers, ResponseContext);
            }

            if (this.options.TraceResponseStatusCode)
            {
                TraceValue(context.Response.StatusCode, $"{ResponseContext}.statusCode");
            }
        }

        private static void TraceRouteData(RouteData routeData, string context)
        {
            if (routeData != null)
            {
                foreach (var item in routeData.Values)
                {
                    TraceValue(item.Value, $"{context}.route.{item.Key}");
                }
            }
        }

        private static void TraceQueryString(IQueryCollection queryCollection, string context)
        {
            if (queryCollection != null)
            {
                foreach (var item in queryCollection)
                {
                    TraceValue(item.Value, $"{context}.query.{item.Key}");
                }
            }
        }

        private static void TraceHeaders(IHeaderDictionary headers, string context)
        {
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    TraceValue(item.Value, $"{context}.headers.{item.Key}");
                }
            }
        }

        private static void TraceCookies(IRequestCookieCollection cookies, string context)
        {
            if (cookies != null)
            {
                foreach (var item in cookies)
                {
                    TraceValue(item.Value, $"{context}.cookies.{item.Key}");
                }
            }
        }

        private static async Task TraceBodyAsync(HttpRequest request, string context)
        {
            if (request.Body != null
                && (request.ContentType?.StartsWith(ApplicationJsonMimeType, StringComparison.InvariantCultureIgnoreCase) ?? false))
            {
                request.EnableBuffering();

                await TraceBodyStreamAsync(request.Body, $"{context}.body").ConfigureAwait(false);
            }
        }

        private static async Task TraceBodyAsync(HttpResponse response, string context, Func<Task> nextExecutor)
        {
            // Copy a pointer to the original response body stream
            var responseBodyStream = response.Body;

            // Create a new memory stream...
            using (var memoryStream = new MemoryStream())
            {
                // ...and use that for the temporary response body
                response.Body = memoryStream;

                await nextExecutor().ConfigureAwait(false);

                // Reset the response body stream position so we can read it
                response.Body.Position = 0;

                if (response.ContentType?.StartsWith(ApplicationJsonMimeType, StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    await TraceBodyStreamAsync(response.Body, $"{context}.body").ConfigureAwait(false);
                }

                // Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                await response.Body.CopyToAsync(responseBodyStream).ConfigureAwait(false);
            }
        }

        private static async Task TraceBodyStreamAsync(Stream stream, string context)
        {
            // Leave the stream open so that the next reader can access it.
            using (var streamReader = new StreamReader(
                stream,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: DefaultStreamReaderBufferSize,
                leaveOpen: true))
            {
                if (!streamReader.EndOfStream)
                {
                    try
                    {
                        using (var jsonReader = new JsonTextReader(streamReader))
                        {
                            var jsonObject = await JToken.LoadAsync(jsonReader).ConfigureAwait(false);

                            TraceJson(jsonObject, context);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Exception: {ex}");
                    }
                    finally
                    {
                        // Reset the stream position so that the next reader can access it.
                        stream.Position = 0;
                    }
                }
            }
        }

        private static void TraceJson(JToken token, string context)
        {
            switch (token.Type)
            {
                case JTokenType.Array:
                    {
                        TraceValue(((JArray)token).Count, $"{context}.count");
                        break;
                    }

                case JTokenType.Object:
                    {
                        foreach (var childToken in token)
                        {
                            if (childToken.Type != JTokenType.Property)
                            {
                                continue;
                            }

                            var property = (JProperty)childToken;

                            TraceJson(property.Value, $"{context}.{property.Name}");
                        }

                        break;
                    }

                case JTokenType.Boolean:
                case JTokenType.Date:
                case JTokenType.Float:
                case JTokenType.Guid:
                case JTokenType.Integer:
                case JTokenType.Null:
                case JTokenType.String:
                case JTokenType.TimeSpan:
                case JTokenType.Undefined:
                case JTokenType.Uri:
                    {
                        TraceValue(token, context);
                        break;
                    }
            }
        }

        private static void TraceValue(object value, string context)
        {
            Trace.WriteLine($"[ParameterMonitor] {context} = {value?.ToString() ?? "nullOrEmpty"}");
        }
    }
}
