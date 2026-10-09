using TobaccoSaaS.Domain.Enums;

namespace TobaccoSaaS.Domain.Common;

/// <summary>
/// Single source of truth for the fixed spec vocabulary strings stored in the database
/// and exposed over the API (spec §19.4, Appendix B). Enum <see cref="string.ToString"/> is
/// never used directly because the spec stores UPPER_SNAKE values while C# enums are PascalCase.
/// </summary>
public static class SpecVocabulary
{
    public static readonly string[] UnitTypes =
    {
        "ORGANIZATION",
        "BUSINESS_UNIT",
        "DEPARTMENT"
    };

    public static string OrgUnitTypeToDb(OrgUnitType type) => type switch
    {
        OrgUnitType.Organization => "ORGANIZATION",
        OrgUnitType.BusinessUnit => "BUSINESS_UNIT",
        OrgUnitType.Department => "DEPARTMENT",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static OrgUnitType OrgUnitTypeFromDb(string value) => value switch
    {
        "ORGANIZATION" => OrgUnitType.Organization,
        "BUSINESS_UNIT" => OrgUnitType.BusinessUnit,
        "DEPARTMENT" => OrgUnitType.Department,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public static string EmployeeStatusToDb(EmployeeStatus status) => status switch
    {
        EmployeeStatus.Active => "ACTIVE",
        EmployeeStatus.Inactive => "INACTIVE",
        EmployeeStatus.Suspended => "SUSPENDED",
        EmployeeStatus.Terminated => "TERMINATED",
        EmployeeStatus.OnLeave => "ON_LEAVE",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static EmployeeStatus EmployeeStatusFromDb(string value) => value switch
    {
        "ACTIVE" => EmployeeStatus.Active,
        "INACTIVE" => EmployeeStatus.Inactive,
        "SUSPENDED" => EmployeeStatus.Suspended,
        "TERMINATED" => EmployeeStatus.Terminated,
        "ON_LEAVE" => EmployeeStatus.OnLeave,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public static string GenderToDb(Gender gender) => gender switch
    {
        Gender.Male => "MALE",
        Gender.Female => "FEMALE",
        Gender.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(gender))
    };

    public static Gender GenderFromDb(string value) => value switch
    {
        "MALE" => Gender.Male,
        "FEMALE" => Gender.Female,
        "OTHER" => Gender.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}