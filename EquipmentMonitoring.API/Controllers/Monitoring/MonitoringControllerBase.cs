using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    public abstract class MonitoringControllerBase : ControllerBase
    {
        protected const string ValidationTitle = "Помилка валідації запиту.";
        protected const string EquipmentNotFoundTitle = "Обладнання не знайдено.";

        protected ObjectResult ValidationFailed(IReadOnlyDictionary<string, List<string>> errors)
        {
            return ProblemResult(StatusCodes.Status400BadRequest, ValidationTitle, errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        protected ObjectResult ValidationFailed(string field, string message)
        {
            return ProblemResult(StatusCodes.Status400BadRequest, ValidationTitle, new Dictionary<string, string[]> { [field] = [message] });
        }

        protected ObjectResult EquipmentNotFound()
        {
            return ProblemResult(
                StatusCodes.Status404NotFound,
                EquipmentNotFoundTitle,
                new Dictionary<string, string[]> { ["equipmentId"] = ["Обладнання з таким ідентифікатором не знайдено."] });
        }

        /// <summary>
        /// Goes through <see cref="ProblemDetailsFactory"/> so responses carry the same
        /// <c>type</c> and <c>traceId</c> as framework-generated validation errors.
        /// </summary>
        protected ObjectResult ProblemResult(int status, string title, Dictionary<string, string[]> errors)
        {
            var modelState = new ModelStateDictionary();

            foreach (var (field, messages) in errors)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            var problem = ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, modelState, status, title);

            return new ObjectResult(problem) { StatusCode = status };
        }

        /// <summary>
        /// Matches enum names only, case-insensitively. Numeric values and
        /// comma-separated flags accepted by <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/> are rejected.
        /// </summary>
        protected static bool TryParseEnumName<TEnum>(string? value, out TEnum result)
            where TEnum : struct, Enum
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var candidate in Enum.GetValues<TEnum>())
            {
                if (string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

        protected static string AllowedValues<TEnum>()
            where TEnum : struct, Enum
        {
            return string.Join(", ", Enum.GetNames<TEnum>());
        }

        /// <summary>
        /// Distinguishes <c>?name=</c> (present but empty) from an absent parameter,
        /// which model binding maps to the same null value.
        /// </summary>
        protected bool IsPresentButEmpty(string queryParameter, object? boundValue)
        {
            return boundValue is null && Request.Query.ContainsKey(queryParameter);
        }
    }
}
