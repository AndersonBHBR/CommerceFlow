namespace CommerceFlow.ServiceDefaults;

public static class SecurityRoles
{
    public const string SalesUser = "sales.user";
    public const string InventoryManager = "inventory.manager";
    public const string Admin = "admin";
}

public static class SecurityPolicies
{
    public const string Authenticated = "authenticated";
    public const string Sales = "sales";
    public const string Inventory = "inventory";
    public const string Administrator = "administrator";
}
