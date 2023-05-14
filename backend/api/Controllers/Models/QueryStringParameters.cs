namespace Api.Controllers.Models
{
    public abstract class QueryStringParameters
    {
        public const string PaginationHeader = "X-Pagination";
        public const int MaxPageSize = 1000;

        /// <summary>
        /// Defaults to '1' if left empty
        /// </summary>
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        /// <summary>
        /// <para>Defaults to '10' if left empty.</para>
        /// Max value is '1000'
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > MaxPageSize) ? MaxPageSize : value; }
        }

        /// <summary>
        /// Can be ordered by several parameters.
        /// <para>Use 'desc' after a parameter name to order it Descending (default is Ascending)</para>
        /// <para>Format: "OrderBy=Id, Name desc, StartTime"</para>
        /// </summary>
        public string OrderBy { get; set; } = "";
    }
}
