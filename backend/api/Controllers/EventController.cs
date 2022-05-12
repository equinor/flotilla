using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("events")]
public class EventController : ControllerBase
{
    private readonly EventService _eventService;
    private readonly RobotService _robotService;

    public EventController(EventService eventService, RobotService robotService)
    {
        _eventService = eventService;
        _robotService = robotService;
    }

    /// <summary>
    /// Gets a list of the events in the database.
    /// </summary>
    /// <remarks>
    /// <para> This query gets all events (paginated) </para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Event>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IList<Event>>> GetEvents()
    {
        var evnts = await _eventService.ReadAll();
        return Ok(evnts);
    }

    /// <summary>
    /// Lookup event with specified id
    /// </summary>
    /// <remarks>
    /// <para> This query gets the event with the specified id </para>
    /// </remarks>
    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Event>> GetEventById([FromRoute] string id)
    {
        var evnt = await _eventService.Read(id);
        if (evnt == null) return NotFound($"Event with id {id} not found");
        return Ok(evnt);
    }

    /// <summary>
    /// Create and add new event to database
    /// </summary>
    /// <remarks>
    /// <para> This query creates a new event and adds it to the database </para>
    /// </remarks>
    [HttpPost]
    [Route("{robotId}/{isarMissionId}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Event>> PostEvent([FromRoute] string robotId, [FromRoute] string isarMissionId)
    {
        var robot = await _robotService.Read(robotId);
        var evnt = new Event();
        evnt.Robot = robot;
        evnt.IsarMissionId = isarMissionId;
        var newEvent = await _eventService.Create(evnt);
        return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);
    }

    /// <summary>
    /// Deletes the event with the specified id from the database.
    /// </summary>
    /// <remarks>
    /// <para> Deletes the event with the specified id from the database </para>
    /// </remarks>
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Event>> DeleteEvent([FromRoute] string id)
    {
        var evnt = await _eventService.Delete(id);
        if (evnt == null) return NotFound($"Event with id {id} not found");
        return Ok(evnt);
    }
}
