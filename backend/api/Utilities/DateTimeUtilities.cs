namespace Api.Utilities
{
    public static class DateTimeUtilities
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var dateTime = epochDateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }
    }
}
