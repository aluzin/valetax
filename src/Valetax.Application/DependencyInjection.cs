using Microsoft.Extensions.DependencyInjection;
using Valetax.Application.Journal.GetRange;
using Valetax.Application.Journal.GetSingle;
using Valetax.Application.Partner.RememberMe;
using Valetax.Application.TreeNodes.CreateNode;
using Valetax.Application.TreeNodes.DeleteNode;
using Valetax.Application.TreeNodes.RenameNode;
using Valetax.Application.Trees.GetTree;

namespace Valetax.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetTreeService, GetTreeService>();
        services.AddScoped<ICreateNodeService, CreateNodeService>();
        services.AddScoped<IRenameNodeService, RenameNodeService>();
        services.AddScoped<IDeleteNodeService, DeleteNodeService>();
        services.AddScoped<IGetJournalRangeService, GetJournalRangeService>();
        services.AddScoped<IGetJournalSingleService, GetJournalSingleService>();
        services.AddScoped<IRememberMeService, RememberMeService>();

        return services;
    }
}
