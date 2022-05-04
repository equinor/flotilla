using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[events]")]
public class EventController : ControllerBase
{
    private readonly ILogger<EventController> _logger;
    private readonly EventService _eventService;

    public EventController(ILogger<EventController> logger, EventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }
    /// <summary>
    /// Gets a list of the events in the database.
    /// </summary>
    /// <remarks>
    /// Responses are paginated.
    ///
    /// Example query:
    ///
    ///     /events?
    ///
    /// <para> This query gets all events (paginated) </para>
    /// </remarks>
    /// <returns> List of events </returns>
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
    /// Responses are paginated.
    ///
    /// Example query:
    ///
    ///     /Event/id
    ///
    /// <para> This query gets the event with the specified id </para>
    /// </remarks>
    /// <returns> Event with specified id </returns>
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
        if (evnt == null)
        {
            return NotFound($"Event with id {id} not found");
        }
        return Ok(evnt);
    }


    /// <summary>
    /// Create and add new event to database
    /// </summary>
    /// <remarks>
    ///
    /// Example query:
    ///
    /// <para> This query creates a new event and adds it to the database </para>
    /// </remarks>
    /// <returns> The created event </returns>
    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Event>> PostEvent([FromBody] Event evnt)
    {
        try
        {
            var newEvent = await _eventService.Create(evnt);
            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to create event");
            throw;
        }
    }
    /// <summary>
    /// Deletes the event with the specified id from the database.
    /// </summary>
    /// <remarks>
    /// <para> Deletes the event with the specified id from the database </para>
    /// </remarks>
    /// <returns> The deleted event </returns>
    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Event>> DeleteEvent([FromRoute] string id)
    {
        try
        {
            var evnt = await _eventService.Delete(id);
            if (evnt == null)
            {
                return NotFound($"Event with id {id} not found");
            }
            return Ok(evnt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to delete event");
            throw;
        }
    }
}
