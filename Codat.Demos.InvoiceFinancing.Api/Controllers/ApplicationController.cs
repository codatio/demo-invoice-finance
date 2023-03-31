using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Orchestrators;
using Microsoft.AspNetCore.Mvc;

namespace Codat.Demos.InvoiceFinancing.Api.Controllers;

[Route("applications")]
[ApiController]
public class ApplicationController : ControllerBase
{
    private readonly IApplicationOrchestrator _applicationOrchestrator;

    public ApplicationController(IApplicationOrchestrator applicationOrchestrator)
    {
        _applicationOrchestrator = applicationOrchestrator;
    }

    /// <summary>
    ///     Start a new invoice financing application.
    /// </summary>
    /// <returns>New application details such as the application ID and Codat Company ID.</returns>
    /// <response code="200">New application details.</response>
    [HttpPost]
    [Route("start")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(NewApplicationDetails), 200)]
    public async Task<NewApplicationDetails> StartApplicationAsync()
    {
        var newApplicationDetails = await _applicationOrchestrator.CreateApplicationAsync();
        return newApplicationDetails;
    }

    /// <summary>
    ///     Get application
    /// </summary>
    /// <param name="applicationId">The invoice financing application ID.</param>
    /// <returns>Returns the application.</returns>
    /// <response code="200">Returns application.</response>
    /// <response code="404">No application exists for application ID.</response>
    [HttpGet]
    [Route("{applicationId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Application), 200)]
    [ProducesResponseType(404)]
    public IActionResult GetApplication([FromRoute] Guid applicationId)
    {
        try
        {
            var application = _applicationOrchestrator.GetApplication(applicationId);
            return Ok(application);
        }
        catch (ApplicationOrchestratorException)
        {
            return NotFound();
        }
    }
}
