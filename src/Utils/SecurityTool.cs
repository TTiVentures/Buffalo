using Buffalo.Models;

namespace Buffalo.Utils
{
    public class SecurityTool
    {
		public static bool VerifyAccess(string? requestUser, string? storedUser, string? storedAccessLevel)
		{
			if (string.IsNullOrEmpty(storedUser) || string.IsNullOrEmpty(storedAccessLevel))
            {
				return true;
            }

			if (storedAccessLevel == AccessModes.PROTECTED.ToString())
			{
				if (requestUser == null)
                {
					return false;
                }
				else if (storedUser != requestUser) {
					return false;
				}
                else
                {
					return true;
                }
			}
            else
            {
				return true;
            }
		}
	}
}
