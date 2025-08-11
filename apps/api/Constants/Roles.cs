namespace MemberOrgApi.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
    
    // Future roles - easy to add
    // public const string Marketing = "Marketing";
    // public const string Finance = "Finance";
    // public const string BoardMember = "BoardMember";
    
    public static readonly string[] AllRoles = new[] { Admin, Member };
    
    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}