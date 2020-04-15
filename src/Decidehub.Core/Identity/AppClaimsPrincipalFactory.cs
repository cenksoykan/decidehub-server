﻿using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Decidehub.Core.Identity
{
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        public AppClaimsPrincipalFactory(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager,
            roleManager, optionsAccessor)
        {
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                ((ClaimsIdentity) principal.Identity).AddClaims(new[]
                    {new Claim(ClaimTypes.GivenName, user.FirstName)});

            if (!string.IsNullOrWhiteSpace(user.LastName))
                ((ClaimsIdentity) principal.Identity).AddClaims(new[] {new Claim(ClaimTypes.Surname, user.LastName)});

            if (!string.IsNullOrWhiteSpace(user.Email))
                ((ClaimsIdentity) principal.Identity).AddClaims(new[] {new Claim(ClaimTypes.Email, user.Email)});

            return principal;
        }
    }
}