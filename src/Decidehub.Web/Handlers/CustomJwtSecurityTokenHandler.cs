using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Decidehub.Core.Helpers;
using Decidehub.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Decidehub.Web.Handlers
{
    public class CustomJwtSecurityTokenHandler : ISecurityTokenValidator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly IUserService _userService;

        public CustomJwtSecurityTokenHandler(IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _tokenHandler = new JwtSecurityTokenHandler();
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool CanValidateToken => true;

        public int MaximumTokenSizeInBytes { get; set; } = TokenValidationParameters.DefaultMaximumTokenSizeInBytes;

        public bool CanReadToken(string securityToken)
        {
            return _tokenHandler.CanReadToken(securityToken);
        }


        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters,
            out SecurityToken validatedToken)
        {
            ClaimsPrincipal result = null;

            var principal = _tokenHandler.ValidateToken(securityToken, validationParameters, out validatedToken);
            Task.WaitAll(Task.Run(async () =>
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    var host = _httpContextAccessor.HttpContext.Request.Host.Value;
                    var domain = UrlParser.GetSubDomain(host);
                    var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimarySid)
                        ?.Value;

                    var user = await _userService.GetUserByIdAndTenant(userId, domain);

                    if (user != null) result = principal;
                }
            }));

            if (result == null) throw new SecurityTokenValidationException();
            return result;
        }
    }
}