using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("submit-feedback")]
    public class FeedbackController(
        ILogger<FeedbackController> logger,
        ITeamsNotificationService teamsNotificationService
    ) : ControllerBase
    {
        /// <summary>
        /// Submit feedback to the Flotilla team
        /// </summary>
        /// <remarks>
        /// <para> This query submits feedback to the Flotilla team </para>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = Role.Any)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult> SubmitFeedback([FromBody] FeedbackQuery feedbackQuery)
        {
            try
            {
                feedbackQuery = Sanitize.SanitizeUserInput(feedbackQuery);
                await teamsNotificationService.SendTeamsMessageAsync(
                    feedbackQuery.ToString(),
                    TeamsDestination.Feedback
                );
                return NoContent();
            }
            catch (TeamsNotificationException e)
            {
                logger.LogError(e, "Error during POST of feedback");
                return StatusCode(StatusCodes.Status502BadGateway);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error during POST of feedback");
                return StatusCode(StatusCodes.Status502BadGateway);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during POST of feedback");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
