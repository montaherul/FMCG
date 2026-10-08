namespace TobaccoSaaS.Domain.Common;

/// <summary>A single permission code of the form <c>module.action</c> (spec §4.2, Appendix B).</summary>
public sealed record PermissionDefinition(string Module, string Code);

/// <summary>
/// The complete default permission catalog from spec Appendix B.
/// This is the single source of truth for seeding and for the authorization test oracle.
/// </summary>
public static class PermissionCatalog
{
    private static readonly Dictionary<string, string[]> Modules = new()
    {
        ["employee"] = new[] { "employee.view", "employee.create", "employee.update", "employee.status", "employee.export" },
        ["user"] = new[] { "user.view", "user.create", "user.update", "user.delete", "user.roles", "user.scope" },
        ["role"] = new[] { "role.view", "role.manage" },
        ["org"] = new[] { "org.view", "org.manage" },
        ["geo"] = new[] { "geo.view", "geo.manage" },
        ["distributor"] = new[] { "distributor.view", "distributor.create", "distributor.update", "distributor.status", "distributor.user.manage", "distributor.export" },
        ["outlet"] = new[] { "outlet.view", "outlet.create", "outlet.update", "outlet.assign", "outlet.import", "outlet.export" },
        ["product"] = new[] { "product.view", "product.create", "product.update", "product.delete", "product.cost.view", "product.price.manage", "product.import" },
        ["route"] = new[] { "route.view", "route.manage" },
        ["attendance"] = new[] { "attendance.view", "attendance.create", "attendance.correct" },
        ["visit"] = new[] { "visit.view", "visit.create", "visit.update", "visit.export" },
        ["sales.order"] = new[] { "sales.order.view", "sales.order.create", "sales.order.submit", "sales.order.approve", "sales.order.cancel", "sales.order.fulfill", "sales.order.export" },
        ["sales.transaction"] = new[] { "sales.transaction.view", "sales.transaction.export" },
        ["target"] = new[] { "target.view", "target.create", "target.allocate", "target.approve", "target.achievement.view", "target.export" },
        ["campaign"] = new[] { "campaign.view", "campaign.create", "campaign.update", "campaign.approve", "campaign.execution.view", "campaign.export" },
        ["trade"] = new[] { "trade.execute", "trade.view", "trade.verify" },
        ["competitor"] = new[] { "competitor.view", "competitor.create", "competitor.export" },
        ["posm"] = new[] { "posm.manage", "posm.view" },
        ["stock"] = new[] { "stock.view", "stock.movement.create", "stock.export" },
        ["invoice"] = new[] { "invoice.view", "invoice.export" },
        ["payment"] = new[] { "payment.view", "payment.create", "payment.void" },
        ["workflow"] = new[] { "workflow.decide", "workflow.manage" },
        ["reports"] = new[] { "reports.view", "reports.export", "reports.schedule" },
        ["audit"] = new[] { "audit.view", "audit.export" },
        ["tenant"] = new[] { "tenant.settings", "tenant.features.manage" },
        ["platform"] = new[] { "platform.tenant.view", "platform.tenant.create", "platform.tenant.suspend", "platform.tenant.plan", "platform.tenant.features", "platform.support.grant", "platform.audit.view" }
    };

    public static IReadOnlyList<PermissionDefinition> All { get; } =
        Modules.SelectMany(kvp => kvp.Value.Select(code => new PermissionDefinition(kvp.Key, code)))
            .ToList();

    public static IReadOnlyList<string> AllCodes { get; } = All.Select(p => p.Code).ToList();

    public static IReadOnlyList<string> ModulesForPlane(bool platform)
        => platform ? new[] { "platform" } : Modules.Keys.Where(m => m != "platform").ToList();

    public static IReadOnlyList<string> CodesForModule(string module)
        => Modules.TryGetValue(module, out var codes) ? codes : Array.Empty<string>();
}
