namespace Api.Controllers.Models
{
    public abstract class QueryStringParameters
    {
        public const string PaginationHeader = "X-Pagination";
        private const int MaxPageSize = 100;

        /// <summary>
        /// Defaults to '1' if left empty
        /// </summary>
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        /// <summary>
        /// <para>Defaults to '10' if left empty.</para>
        /// Max value is '100'
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > MaxPageSize) ? MaxPageSize : value; }
        }
    }
}
