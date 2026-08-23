using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SupplyChainX.Api.Conventions;

/// <summary>
/// Applies '/api/v1' route prefix convention to future business controllers while keeping system endpoints (/health) at root.
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
            // Skip health check system controller
            if (controller.ControllerName.Equals("Health", StringComparison.OrdinalIgnoreCase))
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
