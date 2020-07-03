using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dataflow.SaicEnergyTracker
{
    public class DataTrackerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMsgBatchSender _msgSender;

        private readonly ILogger _logger;
        public DataTrackerMiddleware(RequestDelegate next, IMsgBatchSender msgSender, ILoggerFactory loggerFactory)
        {
            _next = next;
            _msgSender = msgSender;
            _logger = loggerFactory.CreateLogger<DataTrackerMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
                var text = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    await _msgSender.PostMsgsync(text);
                }
                else
                    _logger.LogDebug($"request body is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Data track error,{ex}");
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class DataTrackerMiddlewareExtensions
    {
        public static IApplicationBuilder UseDataTrackerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DataTrackerMiddleware>();
        }
    }
}
