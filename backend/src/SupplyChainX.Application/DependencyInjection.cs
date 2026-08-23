using Microsoft.Extensions.DependencyInjection;

namespace SupplyChainX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application layer registrations (MediatR, FluentValidation, Services) will be added in subsequent milestones.
        return services;
    }
}
