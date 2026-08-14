namespace Api.Utilities
{
    public static class HttpContextExtensions
    {
        public static string? GetUserObjectId(this HttpContext context)
        {
            return context.User?.FindFirst("oid")?.Value;
        }
    }
}
