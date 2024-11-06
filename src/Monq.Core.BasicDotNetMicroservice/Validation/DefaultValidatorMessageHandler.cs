using Calzolari.Grpc.AspNetCore.Validation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Validation;

/// <summary>
/// Default validator message handler for gRPC requests.
/// </summary>
public class DefaultValidatorMessageHandler : IValidatorErrorMessageHandler
{
    /// <inheritdoc/>
    public Task<string> HandleAsync(IList<ValidationFailure> failures)
    {
        return Task.FromResult(string.Join(Environment.NewLine, failures.Select(x => x.ErrorMessage).Distinct()));
    }
}
