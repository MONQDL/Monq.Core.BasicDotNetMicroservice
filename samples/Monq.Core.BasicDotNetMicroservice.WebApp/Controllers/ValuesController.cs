using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.WebApp.ModelsExceptions;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Controllers;

[Route("api/values")]
public class ValuesController : Controller
{
    readonly ILogger<ValuesController> _logger;
    readonly AppConfiguration? _configurationOptions;

    public ValuesController(ILogger<ValuesController> logger, IOptions<AppConfiguration>? configuratiOptions)
    {
        _configurationOptions = configuratiOptions?.Value!;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var model = new { baseUri = _configurationOptions?.BaseUri };
        _logger.LogInformation("Result {result}", model);
        return Ok(model);
    }

    [Authorize]
    [HttpGet("auth")]
    public IActionResult GetAuthenticated()
    {
        var model = new { baseUri = _configurationOptions?.BaseUri };
        _logger.LogInformation("Result {result}", model);
        return Ok(model);
    }

    [HttpGet("error")]
    public string Get(int id) => throw new ResponseException("id is null.", Guid.NewGuid().ToString(),
        System.Net.HttpStatusCode.BadRequest, "{\"data\": [ {\"f\": 1 }, {\"f\": 2 } ]}");
}
