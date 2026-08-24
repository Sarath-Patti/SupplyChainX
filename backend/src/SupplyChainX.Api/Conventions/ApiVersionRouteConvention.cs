using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SupplyChainX.Api.Conventions;

/// <summary>
/// Applies '/api/v1' route prefix convention to domain resource controllers while keeping system endpoints (/health, /api/v1/metrics) at explicit routes.
/// </summary>
public class ApiVersionRouteConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public ApiVersionRouteConvention(string prefix = "api/v1")
    {
        _routePrefix = new AttributeRouteModel { Template = prefix };
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            // Skip health check and metrics system controllers which define absolute routes
            if (controller.ControllerName.Equals("Health", StringComparison.OrdinalIgnoreCase) ||
                controller.ControllerName.Equals("Metrics", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel != null)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        _routePrefix,
                        selector.AttributeRouteModel);
                }
                else
                {
                    selector.AttributeRouteModel = _routePrefix;
                }
            }
        }
    }
}
