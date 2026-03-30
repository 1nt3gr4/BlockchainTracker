using BlockchainTracker.Application.Queries;
using FluentValidation;

namespace BlockchainTracker.Api.Validators;

public class GetChainHistoryRequestValidator : AbstractValidator<GetChainHistoryQuery>
{
    public GetChainHistoryRequestValidator()
    {
        RuleFor(x => x.ChainName).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
