using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.WebApp.ModelsExceptions;
using System;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Controllers
{
    [Route("api/values")]
    public class ValuesController : Controller
    {
        readonly ILogger<ValuesController> _logger;
        readonly AppConfiguration? _configuratiOptions;

        public ValuesController(ILogger<ValuesController> logger, IOptions<AppConfiguration>? configuratiOptions)
        {
            _configuratiOptions = configuratiOptions?.Value!;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var model = new { baseUri = _configuratiOptions?.BaseUri };
            _logger.LogInformation("Result {result}", model);
            return Ok(model);
        }

        [Authorize]
        [HttpGet("auth")]
        public IActionResult GetAuthenticated()
        {
            var model = new { baseUri = _configuratiOptions?.BaseUri };
            _logger.LogInformation("Result {result}", model);
            return Ok(model);
        }

        [HttpGet("error")]
        public string Get(int id) => throw new ResponseException("id is null.", Guid.NewGuid().ToString(),
            System.Net.HttpStatusCode.BadRequest, "{\"data\": [ {\"f\": 1 }, {\"f\": 2 } ]}");
    }
}
