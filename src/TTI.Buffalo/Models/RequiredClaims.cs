using System.Security.Claims;
using System.Text.Json.Serialization;

namespace TTI.Buffalo.Models
{
    public class RequiredClaims
    {
        public RequiredClaimsItem? RootItem { get; set; }
        public bool CheckIdentity(ClaimsIdentity claimsIdentity)
        {
            return CheckItem(claimsIdentity.Claims.ToArray());
        }

        public bool CheckItem(IEnumerable<System.Security.Claims.Claim> claims)
        {
            return RootItem is not null && CheckItem(RootItem, claims);
        }

        private bool CheckItem(RequiredClaimsItem item, IEnumerable<System.Security.Claims.Claim> claims)
        {
            return item switch
            {
                And and => and.Items.All(i => CheckItem(i, claims)),
                Or or => or.Items.Any(i => CheckItem(i, claims)),
                Claim claim => claims.Any(c => c.Type == claim.Key && c.Value == claim.Value),
                _ => false
            };
        }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
    [JsonDerivedType(typeof(Claim), typeDiscriminator: "Claim")]
    [JsonDerivedType(typeof(And), typeDiscriminator: "And")]
    [JsonDerivedType(typeof(Or), typeDiscriminator: "Or")]
    public abstract class RequiredClaimsItem { }
    public class Claim : RequiredClaimsItem
    {
        public Claim(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class And : RequiredClaimsItem
    {
        public List<RequiredClaimsItem> Items { get; set; } = new();
    }
    public class Or : RequiredClaimsItem
    {
        public List<RequiredClaimsItem> Items { get; set; } = new();
    }
}
