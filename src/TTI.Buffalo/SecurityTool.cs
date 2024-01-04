using System.Security.Claims;
using System.Text.Json;
using TTI.Buffalo.Models;

namespace TTI.Buffalo
{
	public class SecurityTool
	{
		public static bool VerifyAccess(ClaimsPrincipal? requestUser, string? storedUser, string? storedAccessLevel, string? storedRequiredClaims = null)
		{
			if (string.IsNullOrEmpty(storedUser) || string.IsNullOrEmpty(storedAccessLevel))
			{
				return true;
			}

            string? userId = null;
            if (requestUser != null && requestUser.Identity != null)
            {
                userId = requestUser.Identity.Name;
            }

            if (Enum.TryParse(typeof(AccessLevels), storedAccessLevel, out var accessLevel))
			{
                bool result = true;

                switch (accessLevel)
                {
                    case AccessLevels.PUBLIC:
                        break;

                    case AccessLevels.ORGANIZATION_OWNED:
						result = requestUser != null;
                        break;

                    case AccessLevels.USER_OWNED:
                        result = userId == storedUser;
                        break;
                    case AccessLevels.CLAIMS:
                        if (storedRequiredClaims == null)
						{
							result = false;
						}
						else
						{
							RequiredClaims? claims = JsonSerializer.Deserialize<RequiredClaims>(storedRequiredClaims);
							if (claims != null && requestUser != null)
							{
								return claims.CheckItem(requestUser.Claims);
							}
						}
                        break;
                }
				return result;
            }
			else
			{
                return false;

            }
		}
	}
}
