using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Common;

/// <summary>A seeded, tenant-editable role template (spec §4.3, Table 4.1).</summary>
public sealed record RoleTemplate(string Code, string Name, RoleScope Scope, IReadOnlyList<string> Permissions);

/// <summary>
/// Default role catalog. Permission bundles are seed defaults and are tenant-editable;
/// the platform role must never be seeded per tenant (spec §4.3, §40.1).
/// </summary>
public static class RoleTemplates
{
    private static string[] AllTenant() =>
        PermissionCatalog.All
            .Where(p => p.Module != "platform")
            .Select(p => p.Code)
            .ToArray();

    private static string[] Modules(params string[] modules) =>
        modules.SelectMany(PermissionCatalog.CodesForModule).ToArray();

    public static readonly IReadOnlyList<RoleTemplate> All = new List<RoleTemplate>
    {
        // Platform plane (tenant_id NULL)
        new("SUPER_ADMIN", "Super Admin", RoleScope.Platform, PermissionCatalog.CodesForModule("platform")),
        new("PLATFORM_ADMIN", "Platform Admin", RoleScope.Platform, PermissionCatalog.CodesForModule("platform")),
        new("PLATFORM_SUPPORT", "Platform Support", RoleScope.Platform, new[] { "platform.tenant.view", "platform.support.grant" }),

        // Tenant executive
        new("TENANT_ADMIN", "Tenant Admin", RoleScope.Tenant, AllTenant()),
        new("CEO", "CEO", RoleScope.Tenant, Modules("employee", "user", "org", "geo", "distributor", "outlet", "product", "route", "visit", "sales.order", "sales.transaction", "target", "campaign", "trade", "competitor", "posm", "stock", "invoice", "payment", "reports", "audit")),
        new("COMMERCIAL_DIRECTOR", "Commercial Director", RoleScope.Tenant, Modules("employee", "distributor", "outlet", "product", "route", "visit", "sales.order", "sales.transaction", "target", "campaign", "trade", "competitor", "stock", "invoice", "payment", "reports")),

        // Sales ladder
        new("HEAD_OF_SALES", "Head of Sales", RoleScope.Tenant, Modules("employee", "geo", "distributor", "outlet", "product", "route", "visit", "sales.order", "sales.transaction", "target", "campaign", "competitor", "stock", "invoice", "payment", "reports")),
        new("REGIONAL_MANAGER", "Regional Manager", RoleScope.Tenant, Modules("geo", "distributor", "outlet", "route", "visit", "sales.order", "sales.transaction", "target", "campaign", "competitor", "stock", "invoice", "payment", "reports")),
        new("AREA_MANAGER", "Area Manager", RoleScope.Tenant, Modules("outlet", "route", "visit", "sales.order", "target", "competitor", "stock", "reports")),
        new("TERRITORY_OFFICER", "Territory Officer", RoleScope.Tenant, Modules("outlet", "route", "attendance", "visit", "sales.order", "target", "competitor", "stock", "reports")),
        new("FIELD_SUPERVISOR", "Field Supervisor", RoleScope.Tenant, Modules("employee", "outlet", "route", "attendance", "visit", "sales.order", "competitor")),
        new("CSR", "Consumer Sales Representative", RoleScope.Tenant, new[]
        {
            "outlet.view", "visit.view", "visit.create", "visit.update",
            "route.view", "attendance.view", "attendance.create",
            "sales.order.view", "sales.order.create", "sales.order.submit",
            "stock.view", "stock.movement.create", "competitor.view", "competitor.create",
            "payment.view", "payment.create"
        }),

        // Marketing
        new("HEAD_OF_MARKETING", "Head of Marketing", RoleScope.Tenant, Modules("product", "outlet", "visit", "sales.transaction", "campaign", "trade", "competitor", "posm", "reports")),
        new("BRAND_MANAGER", "Brand Manager", RoleScope.Tenant, Modules("product", "campaign", "competitor", "reports")),
        new("TRADE_MARKETING_MANAGER", "Trade Marketing Manager", RoleScope.Tenant, Modules("outlet", "visit", "campaign", "trade", "competitor", "posm", "reports")),
        new("TRADE_MARKETING_OFFICER", "Trade Marketing Officer", RoleScope.Tenant, Modules("outlet", "visit", "trade", "competitor", "posm")),
        new("MARKETING_EXECUTIVE", "Marketing Executive", RoleScope.Tenant, Modules("campaign", "trade", "competitor", "posm")),
        new("MARKETING_ANALYST", "Marketing Analyst", RoleScope.Tenant, new[] { "campaign.view", "campaign.export", "competitor.view", "competitor.export", "sales.transaction.view", "reports.view", "reports.export", "reports.schedule" }),

        // Distribution portal
        new("DISTRIBUTOR_ADMIN", "Distributor Admin", RoleScope.Tenant, new[] { "distributor.view", "distributor.update", "distributor.user.manage", "sales.order.view", "sales.order.fulfill", "stock.view", "stock.movement.create", "invoice.view", "payment.view", "payment.create" }),
        new("DISTRIBUTOR_MANAGER", "Distributor Manager", RoleScope.Tenant, new[] { "distributor.view", "sales.order.view", "sales.order.fulfill", "stock.view", "stock.movement.create", "invoice.view", "payment.view" }),
        new("DISTRIBUTOR_STAFF", "Distributor Staff", RoleScope.Tenant, new[] { "distributor.view", "sales.order.view", "stock.view", "invoice.view" }),

        // Analytics
        new("SALES_ANALYST", "Sales Analyst", RoleScope.Tenant, new[] { "sales.order.view", "sales.order.export", "sales.transaction.view", "sales.transaction.export", "target.view", "target.achievement.view", "target.export", "product.view", "outlet.view", "outlet.export", "distributor.view", "distributor.export", "reports.view", "reports.export", "reports.schedule" })
    };
}
