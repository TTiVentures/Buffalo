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

            if (storedAccessLevel == AccessLevels.PROTECTED.ToString())
            {
                if (string.IsNullOrEmpty(requestUser))
                {
                    return false;
                }
                else if (storedUser != requestUser)
                {
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
