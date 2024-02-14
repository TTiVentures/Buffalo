using System.Security.Claims;
using System.Text.Json.Serialization;

namespace TTI.Buffalo.Models;

public class SecurityClaims
{
    public required SecurityItem RootItem { get; set; }

    public bool CheckIdentity(ClaimsIdentity claimsIdentity)
    {
        return CheckClaims(claimsIdentity.Claims.ToArray());
    }

    public bool CheckClaims(IEnumerable<Claim> claims)
    {
        return CheckClaims(RootItem, claims);
    }

    private bool CheckClaims(SecurityItem item, IEnumerable<Claim> claims)
    {
        return item switch
        {
            And and => and.Items.All(i => CheckClaims(i, claims)),
            Or or => or.Items.Any(i => CheckClaims(i, claims)),
            ClaimItem claim => claims.Any(c => c.Type == claim.Key && c.Value == claim.Value),
            _ => false
        };
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(ClaimItem), "Claim")]
[JsonDerivedType(typeof(And), "And")]
[JsonDerivedType(typeof(Or), "Or")]
public abstract class SecurityItem { }

public class ClaimItem : SecurityItem
{
    [JsonConstructor]
    public ClaimItem() { }

    public ClaimItem(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class And : SecurityItem
{
    public List<SecurityItem> Items { get; set; } = new();
}

public class Or : SecurityItem
{
    public List<SecurityItem> Items { get; set; } = new();
}