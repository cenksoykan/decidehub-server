using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AutoMapper;
using Decidehub.Core.Identity;
using Decidehub.Core.Interfaces;
using Decidehub.Core.Services;
using Decidehub.Infrastructure.Data;
using Decidehub.Infrastructure.Data.Repositories;
using Decidehub.Infrastructure.Services;
using Decidehub.Web.Handlers;
using Decidehub.Web.Interfaces;
using Decidehub.Web.Models;
using Decidehub.Web.Services;
using Hangfire;
using Hangfire.Console;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Storage;
using RecurringJob = Decidehub.Web.RecurringJobs.RecurringJob;

namespace Decidehub.Web
{
    public class Startup
    {
        private const string SecretKey = "iNivDmHLpUA223sqsfhqGbMRdRj1PVkH";

        private readonly SymmetricSecurityKey
            _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy",
                    policy => { policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
            });


            services.AddDbContext<TenantsDbContext>(options =>
                options.UseNpgsql(Configuration["PostgreSqlConnection"] ?? ""));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration["PostgreSqlConnection"] ?? ""));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory>();

            services.AddHangfire(x =>
            {
                x.UsePostgreSqlStorage(Configuration["PostgreSqlConnection"]);
                x.UseConsole();
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(30);
            });

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            RegisterAppServices(services);

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddMiniProfiler(options =>
                    // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
                {
                    options.RouteBasePath = "/profiler";

                    // (Optional) Control storage
                    // (default is 30 minutes in MemoryCacheStorage)
                    ((MemoryCacheStorage) options.Storage).CacheDuration = TimeSpan.FromMinutes(60);

                    // (Optional) Control which SQL formatter to use, InlineFormatter is the default
                    options.SqlFormatter = new InlineFormatter();
                })
                .AddEntityFramework();


            ConfigureJwt(services);

            services.AddMvc()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix,
                    opts => { opts.ResourcesPath = "Resources"; })
                .AddDataAnnotationsLocalization();
            services.AddAutoMapper(typeof(Startup));
            AddSwagger(services);
        }

        private static void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Decidehub API",
                    Description = "Decidehub Web API Version 1",
                    Contact = new OpenApiContact {Name = "Workhow", Email = "support@workhow.com"}
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                var basePath = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(basePath, "Decidehub.Web.xml");
                c.IncludeXmlComments(xmlPath);
            });
        }


        private void ConfigureJwt(IServiceCollection services)
        {
            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,

                RequireExpirationTime = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            var sp = services.BuildServiceProvider();
            var service = sp.GetService<CustomJwtSecurityTokenHandler>();
            services.AddAuthentication()
                .AddJwtBearer(configureOptions =>
                {
                    configureOptions.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                    configureOptions.TokenValidationParameters = tokenValidationParameters;
                    configureOptions.SaveToken = true;
                    configureOptions.SecurityTokenValidators.Clear();
                    configureOptions.SecurityTokenValidators.Add(service);
                });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseMiniProfiler();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();
            app.UseAuthorization();
            //comment this when you don't want to migrate db on app start
            InitializeDatabase(app);

            app.UseHangfireServer();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            var supportedCultures = new[]
            {
                //   new CultureInfo("en"),
                //   new CultureInfo("en-US"),
                new CultureInfo("tr"),
                new CultureInfo("tr-TR")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("tr-TR"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });

            app.UseSwagger();

            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Decidehub API V1"); });

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            SetupRecurringJobs(app);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tenantDb = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
                db.Database.Migrate();
                tenantDb.Database.Migrate();
            }
        }

        private void RegisterAppServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

            services.AddScoped<IUserRepository, EntityUserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IPollRepository, EntityPollRepository>();
            services.AddScoped<IPollService, PollService>();
            services.AddScoped<IPolicyService, PolicyService>();
            services.AddScoped<IPollApiViewModelService, PollApiViewModelService>();
            services.AddScoped<IVoteService, VoteService>();
            services.AddScoped<IPollJobService, PollJobService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<ITenantProvider, TenantProvider>();
            services.AddScoped<ITenantRepository, EntityTenantRepository>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IGenericService, GenericService>();
            services.AddScoped<IUserApiViewModelService, UserApiViewModelService>();
            services.AddScoped<CustomJwtSecurityTokenHandler>();
            var allJobs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(RecurringJob)) && !type.IsAbstract);
            foreach (var item in allJobs) services.AddScoped(typeof(RecurringJob), item);
        }


        private void SetupRecurringJobs(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var services = scope.ServiceProvider.GetServices<RecurringJob>();
                var culture = "tr-TR";

                foreach (var recurringJob in services)
                    Hangfire.RecurringJob.AddOrUpdate(() => recurringJob.RunWithCulture(null, culture),
                        recurringJob.CronExpression);
            }
        }
    }
}