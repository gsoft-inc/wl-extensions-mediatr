using System.ComponentModel.DataAnnotations;

namespace Workleap.Extensions.MediatR;

internal static class DataAnnotationsValidationHelper
{
    // Inspired from .NET options validation
    // https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/Microsoft.Extensions.Options.DataAnnotations/src/DataAnnotationValidateOptions.cs
    public static void Validate(object request)
    {
        var validationResults = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
        {
            return;
        }

        var requestType = request.GetType();
        var requestName = requestType.Name;
        var failureMessages = new List<string>(validationResults.Count);

        foreach (var result in validationResults)
        {
            failureMessages.Add($"Validation failed for '{requestName}' members: '{string.Join(",", result.MemberNames)}' with the error: '{result.ErrorMessage}'.");
        }

        throw new RequestValidationException(requestName, requestType, failureMessages);
    }
}