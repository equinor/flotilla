using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;
using Api.Database.Models;

namespace Api.Services
{
    public class SortingService
    {
        public static void ApplySort<T>(ref IQueryable<T> missions, string orderByQueryString) where T : SortableRecord
        {
            if (!missions.Any())
                return;

            if (string.IsNullOrWhiteSpace(orderByQueryString))
            {
                missions = missions.OrderBy(x => x.Name);
                return;
            }

            string[] orderParams = orderByQueryString
                .Trim()
                .Split(',')
                .Select(parameterString => parameterString.Trim())
                .ToArray();

            var propertyInfos = typeof(T).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            );
            var orderQueryBuilder = new StringBuilder();

            foreach (string param in orderParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                string propertyFromQueryName = param.Split(" ")[0];
                var objectProperty = propertyInfos.FirstOrDefault(
                    pi =>
                        pi.Name.Equals(
                            propertyFromQueryName,
                            StringComparison.Ordinal)
                );

                if (objectProperty == null)
                    throw new InvalidDataException(
                        $"Mission has no property '{propertyFromQueryName}' for ordering"
                    );

                string sortingOrder = param.EndsWith(" desc", StringComparison.OrdinalIgnoreCase)
                  ? "descending"
                  : "ascending";

                string sortParameter = $"{objectProperty.Name} {sortingOrder}, ";
                orderQueryBuilder.Append(sortParameter);
            }

            string orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

            missions = string.IsNullOrWhiteSpace(orderQuery)
              ? missions.OrderBy(mission => mission.Name)
              : missions.OrderBy(orderQuery);
        }
    }
}
