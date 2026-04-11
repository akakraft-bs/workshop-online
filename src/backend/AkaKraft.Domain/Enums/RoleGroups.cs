namespace AkaKraft.Domain.Enums;

public static class RoleGroups
{
    public static readonly Role[] Vorstand =
    [
        Role.Getraenkewart,
        Role.Grillwart,
        Role.Hallenwart,
        Role.Veranstaltungswart,
        Role.Treasurer,
        Role.ViceChairman,
        Role.Chairman,
    ];

    public static bool IsVorstand(this Role role) => Vorstand.Contains(role);
}
