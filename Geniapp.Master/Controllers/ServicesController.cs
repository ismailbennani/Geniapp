using Geniapp.Master.Models;
using Geniapp.Master.Services;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Geniapp.Master.Controllers;

[Route("/admin/services")]
[OpenApiTag("Services")]
[ApiController]
public class ServicesController : ControllerBase
{
    [HttpGet("frontend")]
    public IReadOnlyCollection<FrontendServiceInformation> GetFrontendServices([FromServices] FrontendsService frontendsService) => frontendsService.GetFrontendServices();

    [HttpGet("worker")]
    public IReadOnlyCollection<WorkerServiceInformation> GetWorkerServices([FromServices] WorkersService workerServices) => workerServices.GetWorkerServices();
}
