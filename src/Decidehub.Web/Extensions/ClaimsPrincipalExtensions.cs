using System;
using System.Security.Claims;

namespace Decidehub.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get User Id from claims
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Get User Id from claims
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ApiGetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.PrimarySid)?.Value;
        }
    }
}