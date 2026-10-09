using System.Linq.Expressions;
using TobaccoSaaS.Application.Common.Exceptions;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Domain.Entities.Audit;
using TobaccoSaaS.Domain.Entities.Identity;
using TobaccoSaaS.Domain.Entities.Organization;
using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Application.Features.Organization;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditWriter _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public OrganizationService(
        IUnitOfWork uow,
        IAuditWriter audit,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _uow = uow;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<OrgUnitDto>> ListUnitsAsync(int page, int pageSize, string? q, string? unitType, Guid? parentId, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var term = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var type = ParseUnitType(unitType);

        Expression<Func<OrgUnit, bool>> predicate = u =>
            u.TenantId == tenantId &&
            (term == null || u.Name.Contains(term) || (u.Code != null && u.Code.Contains(term))) &&
            (type == null || u.UnitType == type) &&
            (parentId == null || u.ParentId == parentId);

        var (p, s) = Normalize(page, pageSize);
        var result = await _uow.Repository<OrgUnit>().PagedAsync(predicate, p, s, ct);
        return new PagedResult<OrgUnitDto>(
            result.Items.Select(ToUnitDto).OrderBy(o => o.Name).ToList(),
            result.Page, result.PageSize, result.Total);
    }

    public async Task<List<OrgUnitTreeNodeDto>> GetUnitTreeAsync(CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var units = await _uow.Repository<OrgUnit>().ListAsync(u => u.TenantId == tenantId, ct);
        var ids = units.Select(u => u.Id).ToHashSet();

        var childrenByParent = units
            .Where(u => u.ParentId is Guid parent && ids.Contains(parent))
            .GroupBy(u => u.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());

        OrgUnitTreeNodeDto Build(OrgUnit unit)
        {
            var children = childrenByParent.TryGetValue(unit.Id, out var kids)
                ? kids.Select(Build).ToList()
                : new List<OrgUnitTreeNodeDto>();
            return new OrgUnitTreeNodeDto(unit.Id, SpecVocabulary.OrgUnitTypeToDb(unit.UnitType), unit.ParentId, unit.Name, unit.Code, children);
        }

        return units
            .Where(u => u.ParentId is null || !ids.Contains(u.ParentId.Value))
            .OrderBy(u => u.UnitType).ThenBy(u => u.Name)
            .Select(Build)
            .ToList();
    }

    public async Task<OrgUnitDto> GetUnitAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var unit = await _uow.Repository<OrgUnit>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Organization unit not found.");
        EnsureTenant(unit, tenantId);
        return ToUnitDto(unit);
    }

    public async Task<OrgUnitDto> CreateUnitAsync(CreateOrgUnitRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var type = ParseUnitType(request.UnitType)
            ?? throw new BusinessRuleException("UnitType must be one of: " + string.Join(", ", SpecVocabulary.UnitTypes));

        var unit = new OrgUnit
        {
            TenantId = tenantId,
            UnitType = type,
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        if (request.ParentId is Guid parentId)
        {
            await ResolveUnitParentAsync(parentId, tenantId, ct);
        }

        await _uow.Repository<OrgUnit>().AddAsync(unit, ct);
        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("org.unit.create", "OrgUnit", unit.Id.ToString(), $"Created {SpecVocabulary.OrgUnitTypeToDb(type)} '{unit.Name}'.", ct);
        return ToUnitDto(unit);
    }

    public async Task<OrgUnitDto> UpdateUnitAsync(Guid id, UpdateOrgUnitRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var unit = await _uow.Repository<OrgUnit>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Organization unit not found.");
        EnsureTenant(unit, tenantId);

        var type = ParseUnitType(request.UnitType)
            ?? throw new BusinessRuleException("UnitType must be one of: " + string.Join(", ", SpecVocabulary.UnitTypes));
        var oldParentId = unit.ParentId;

        if (request.ParentId is Guid parentId)
        {
            await ResolveUnitParentAsync(parentId, tenantId, ct);
            await EnsureNoUnitCycleAsync(unit.Id, parentId, ct);
        }

        unit.UnitType = type;
        unit.ParentId = request.ParentId;
        unit.Name = request.Name.Trim();
        unit.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        unit.UpdatedBy = _currentUser.UserId;
        _uow.Repository<OrgUnit>().Update(unit);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("org.unit.update", "OrgUnit", unit.Id.ToString(),
            $"Updated '{unit.Name}' (parent {oldParentId} -> {request.ParentId}).", ct);
        return ToUnitDto(unit);
    }

    public async Task DeleteUnitAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var unit = await _uow.Repository<OrgUnit>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Organization unit not found.");
        EnsureTenant(unit, tenantId);

        if (await _uow.Repository<OrgUnit>().AnyAsync(u => u.ParentId == id, ct))
        {
            throw new BusinessRuleException("Cannot delete an organization unit that still has child units.");
        }

        _uow.Repository<OrgUnit>().Remove(unit);
        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("org.unit.delete", "OrgUnit", unit.Id.ToString(), $"Deleted unit '{unit.Name}'.", ct);
    }

    public async Task<PagedResult<PositionDto>> ListPositionsAsync(int page, int pageSize, string? q, Guid? orgUnitId, bool? isActive, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var term = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        Expression<Func<Position, bool>> predicate = p =>
            p.TenantId == tenantId &&
            (term == null || p.Title.Contains(term)) &&
            (orgUnitId == null || p.OrgUnitId == orgUnitId) &&
            (isActive == null || p.IsActive == isActive);

        var (p, s) = Normalize(page, pageSize);
        var result = await _uow.Repository<Position>().PagedAsync(predicate, p, s, ct);
        return new PagedResult<PositionDto>(
            result.Items.Select(ToPositionDto).OrderBy(x => x.Title).ToList(),
            result.Page, result.PageSize, result.Total);
    }

    public async Task<PositionDto> GetPositionAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var position = await _uow.Repository<Position>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Position not found.");
        EnsureTenant(position, tenantId);
        return ToPositionDto(position);
    }

    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var unit = await _uow.Repository<OrgUnit>().GetByIdAsync(request.OrgUnitId, ct)
            ?? throw new NotFoundException("Organization unit not found.");
        EnsureTenant(unit, tenantId);

        if (request.DefaultRoleId is Guid roleId)
        {
            await ResolveTenantRoleAsync(roleId, tenantId, ct);
        }

        var position = new Position
        {
            TenantId = tenantId,
            OrgUnitId = request.OrgUnitId,
            Title = request.Title.Trim(),
            ReportsTo = request.ReportsTo,
            DefaultRoleId = request.DefaultRoleId,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        if (request.ReportsTo is Guid reportId)
        {
            var manager = await _uow.Repository<Position>().GetByIdAsync(reportId, ct)
                ?? throw new NotFoundException("Report-to position not found.");
            EnsureTenant(manager, tenantId);
            await EnsureNoPositionCycleAsync(position.Id, manager, ct);
        }

        await _uow.Repository<Position>().AddAsync(position, ct);
        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("org.position.create", "Position", position.Id.ToString(), $"Created position '{position.Title}'.", ct);
        return ToPositionDto(position);
    }

    public async Task<PositionDto> UpdatePositionAsync(Guid id, UpdatePositionRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var position = await _uow.Repository<Position>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Position not found.");
        EnsureTenant(position, tenantId);

        var unit = await _uow.Repository<OrgUnit>().GetByIdAsync(request.OrgUnitId, ct)
            ?? throw new NotFoundException("Organization unit not found.");
        EnsureTenant(unit, tenantId);

        if (request.DefaultRoleId is Guid roleId)
        {
            await ResolveTenantRoleAsync(roleId, tenantId, ct);
        }

        if (request.ReportsTo == position.Id)
        {
            throw new BusinessRuleException("A position cannot report to itself.");
        }

        if (request.ReportsTo is Guid reportId)
        {
            var manager = await _uow.Repository<Position>().GetByIdAsync(reportId, ct)
                ?? throw new NotFoundException("Report-to position not found.");
            EnsureTenant(manager, tenantId);
            await EnsureNoPositionCycleAsync(position.Id, manager, ct);
        }

        if (!request.IsActive && position.IsActive)
        {
            await EnsureNoActiveIncumbentsAsync(position.Id, ct);
        }

        position.OrgUnitId = request.OrgUnitId;
        position.Title = request.Title.Trim();
        position.ReportsTo = request.ReportsTo;
        position.DefaultRoleId = request.DefaultRoleId;
        position.IsActive = request.IsActive;
        position.UpdatedBy = _currentUser.UserId;
        _uow.Repository<Position>().Update(position);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("org.position.update", "Position", position.Id.ToString(),
            $"Updated position '{position.Title}' (active={request.IsActive}).", ct);
        return ToPositionDto(position);
    }

    public async Task DeletePositionAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var position = await _uow.Repository<Position>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Position not found.");
        EnsureTenant(position, tenantId);

        await EnsureNoActiveIncumbentsAsync(position.Id, ct);

        _uow.Repository<Position>().Remove(position);
        await _uow.SaveChangesAsync(ct);
        await WriteAuditAsync("org.position.delete", "Position", position.Id.ToString(), $"Deleted position '{position.Title}'.", ct);
    }

    public async Task<EmployeePositionDto> AssignEmployeeAsync(Guid positionId, AssignEmployeePositionRequest request, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var position = await _uow.Repository<Position>().GetByIdAsync(positionId, ct)
            ?? throw new NotFoundException("Position not found.");
        EnsureTenant(position, tenantId);
        if (!position.IsActive)
        {
            throw new BusinessRuleException("Cannot assign an employee to an inactive position.");
        }

        var employee = await _uow.Repository<Employee>().GetByIdAsync(request.EmployeeId, ct)
            ?? throw new NotFoundException("Employee not found.");
        EnsureTenant(employee, tenantId);

        if (request.IsPrimary)
        {
            await EnsureNoConflictingPrimaryAsync(employee.Id, positionId, request.ValidFrom, request.ValidTo, ct);
        }

        var assignment = new EmployeePosition
        {
            TenantId = tenantId,
            EmployeeId = employee.Id,
            PositionId = position.Id,
            IsPrimary = request.IsPrimary,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _uow.Repository<EmployeePosition>().AddAsync(assignment, ct);
        await _uow.SaveChangesAsync(ct);

        await WriteAuditAsync("org.employee_position.assign", "EmployeePosition", assignment.Id.ToString(),
            $"Assigned {employee.FullName} to '{position.Title}' (primary={request.IsPrimary}).", ct);
        return new EmployeePositionDto(assignment.Id, employee.Id, employee.FullName, position.Id, assignment.IsPrimary, assignment.ValidFrom, assignment.ValidTo);
    }

    public async Task<List<EmployeePositionDto>> ListAssignmentsAsync(Guid positionId, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var assignments = await _uow.Repository<EmployeePosition>()
            .ListAsync(a => a.TenantId == tenantId && a.PositionId == positionId, ct);

        var employeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList();
        var employees = employeeIds.Count == 0
            ? new Dictionary<Guid, Employee>()
            : (await _uow.Repository<Employee>().ListAsync(e => employeeIds.Contains(e.Id), ct)).ToDictionary(e => e.Id);

        return assignments
            .OrderByDescending(a => a.ValidFrom)
            .Select(a => new EmployeePositionDto(a.Id, a.EmployeeId,
                employees.TryGetValue(a.EmployeeId, out var employee) ? employee.FullName : string.Empty,
                a.PositionId, a.IsPrimary, a.ValidFrom, a.ValidTo))
            .ToList();
    }

    public async Task ReleaseAssignmentAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var assignment = await _uow.Repository<EmployeePosition>().GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Assignment not found.");
        EnsureTenant(assignment, tenantId);

        if (assignment.ValidTo is null || assignment.ValidTo >= DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime))
        {
            assignment.ValidTo = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
            assignment.UpdatedBy = _currentUser.UserId;
            _uow.Repository<EmployeePosition>().Update(assignment);
            await _uow.SaveChangesAsync(ct);
        }

        await WriteAuditAsync("org.employee_position.release", "EmployeePosition", assignment.Id.ToString(),
            $"Released assignment ending at {assignment.ValidTo}.", ct);
    }

    public async Task<List<ApproverChainNodeDto>> GetApproverChainAsync(Guid positionId, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        var position = await _uow.Repository<Position>().GetByIdAsync(positionId, ct)
            ?? throw new NotFoundException("Position not found.");
        EnsureTenant(position, tenantId);

        var nodes = new List<ApproverChainNodeDto>();
        var visited = new HashSet<Guid>();
        var currentId = position.ReportsTo;
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        while (currentId is Guid cursor)
        {
            if (!visited.Add(cursor))
            {
                break;
            }

            var manager = await _uow.Repository<Position>().GetByIdAsync(cursor, ct);
            if (manager is null || manager.TenantId != tenantId)
            {
                break;
            }

            var vacant = !await HasActiveIncumbentAsync(manager.Id, today, ct);
            nodes.Add(new ApproverChainNodeDto(manager.Id, manager.Title, manager.ReportsTo, vacant));
            currentId = manager.ReportsTo;
        }

        return nodes;
    }

    private async Task<OrgUnit> ResolveUnitParentAsync(Guid parentId, Guid tenantId, CancellationToken ct)
    {
        var parent = await _uow.Repository<OrgUnit>().GetByIdAsync(parentId, ct)
            ?? throw new NotFoundException("Parent organization unit not found.");
        EnsureTenant(parent, tenantId);
        return parent;
    }

    private async Task<Role> ResolveTenantRoleAsync(Guid roleId, Guid tenantId, CancellationToken ct)
    {
        var role = await _uow.Repository<Role>().GetByIdAsync(roleId, ct)
            ?? throw new NotFoundException("Default role not found.");
        if (role.TenantId != tenantId)
        {
            throw new ForbiddenException("The default role must belong to the current tenant.");
        }

        return role;
    }

    private async Task EnsureNoUnitCycleAsync(Guid unitId, Guid newParentId, CancellationToken ct)
    {
        if (newParentId == unitId)
        {
            throw new BusinessRuleException("An organization unit cannot be its own parent.");
        }

        var visited = new HashSet<Guid> { unitId };
        var cursor = newParentId;
        while (cursor != Guid.Empty)
        {
            if (!visited.Add(cursor))
            {
                throw new BusinessRuleException("Moving this unit would create a cycle in the organization hierarchy.");
            }

            var node = await _uow.Repository<OrgUnit>().GetByIdAsync(cursor, ct);
            cursor = node?.ParentId ?? Guid.Empty;
        }
    }

    private async Task EnsureNoPositionCycleAsync(Guid positionId, Position manager, CancellationToken ct)
    {
        if (manager.ReportsTo == positionId)
        {
            throw new BusinessRuleException("Report-to links cannot cycle back to the position.");
        }

        var visited = new HashSet<Guid> { positionId };
        var cursor = manager.ReportsTo;
        while (cursor is Guid next)
        {
            if (!visited.Add(next))
            {
                throw new BusinessRuleException("Report-to links would create a cycle.");
            }

            var node = await _uow.Repository<Position>().GetByIdAsync(next, ct);
            cursor = node?.ReportsTo;
        }
    }

    private async Task EnsureNoActiveIncumbentsAsync(Guid positionId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        if (await HasActiveIncumbentAsync(positionId, today, ct))
        {
            throw new BusinessRuleException("Cannot deactivate a position that still has active incumbents; release or reassign them first.");
        }
    }

    private async Task<bool> HasActiveIncumbentAsync(Guid positionId, DateOnly today, CancellationToken ct)
        => await _uow.Repository<EmployeePosition>().AnyAsync(a =>
            a.PositionId == positionId &&
            a.ValidFrom <= today &&
            (a.ValidTo == null || a.ValidTo >= today), ct);

    private async Task EnsureNoConflictingPrimaryAsync(Guid employeeId, Guid positionId, DateOnly validFrom, DateOnly? validTo, CancellationToken ct)
    {
        // Overlap if each range starts before the other ends; a null ValidTo is an open end.
        var effectiveValidTo = validTo ?? DateOnly.MaxValue;
        var conflicting = await _uow.Repository<EmployeePosition>().AnyAsync(a =>
            a.EmployeeId == employeeId &&
            a.PositionId != positionId &&
            a.IsPrimary &&
            a.ValidFrom <= effectiveValidTo &&
            (a.ValidTo == null || a.ValidTo >= validFrom), ct);

        if (conflicting)
        {
            throw new BusinessRuleException("The employee already holds a primary position overlapping that validity range.");
        }
    }

    private Guid ResolveTenantId()
        => _currentUser.TenantId ?? throw new ForbiddenException("The organization module requires a tenant context.");

    private static void EnsureTenant(ITenantEntity entity, Guid tenantId)
    {
        if (entity.TenantId != tenantId)
        {
            throw new NotFoundException("The requested resource was not found.");
        }
    }

    private static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;
        return (page, pageSize);
    }

    private static OrgUnitType? ParseUnitType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        foreach (var type in Enum.GetValues<OrgUnitType>())
        {
            if (string.Equals(SpecVocabulary.OrgUnitTypeToDb(type), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return null;
    }

    private static OrgUnitDto ToUnitDto(OrgUnit unit) =>
        new(unit.Id, SpecVocabulary.OrgUnitTypeToDb(unit.UnitType), unit.ParentId, unit.Name, unit.Code, unit.CreatedAt);

    private static PositionDto ToPositionDto(Position position) =>
        new(position.Id, position.OrgUnitId, position.Title, position.ReportsTo, position.DefaultRoleId, position.IsActive, position.CreatedAt);

    private Task WriteAuditAsync(string action, string entityType, string entityId, string detail, CancellationToken ct)
        => _audit.WriteAsync(new AuditLog
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId,
            Action = action,
            Module = "Organization",
            EntityType = entityType,
            EntityId = entityId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { detail }),
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent
        }, ct);
}