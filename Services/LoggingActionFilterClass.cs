using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;

public class LoggingActionFilter : IActionFilter
{
    private readonly ILogger  _logger;

    public LoggingActionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(LoggingActionFilter));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        return;
    }

    public void OnActionExecuting(ActionExecutingContext filterContext)
    {
        _logger.LogInformation(new EventId(100,"Operation Request"),null,$"Request.QueryString:{filterContext.HttpContext.Request.QueryString}");
    }
}