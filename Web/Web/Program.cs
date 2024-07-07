using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Business.Authenticate.AuthorizationRequirement;
using Business.Authenticate.TokenProvider;
using Business.Business.Interfaces.User;
using Business.Business.Repositories.User;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.User;
using Business.KeyManagement;
using Business.Models;
using BusinessModels.General;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Protector;
using Protector.Certificates.Models;
using Protector.KeyProvider;
using Protector.Tracer;
using Web.Authenticate;
using Web.Components;
using Web.MiddleWares;
using Web.Services;
using _Imports=Web.Client._Imports;

namespace Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization();

        #region Configure Setting

        builder.Services.Configure<DbSettingModel>(builder.Configuration.GetSection("DBSetting"));
        builder.Services.Configure<AppCertificate>(builder.Configuration.GetSection("AppCertificate"));

        #endregion


        #region Additionnal services

        // builder.Services.AddScoped<MongoDbEntityClient>();

        builder.Services.AddSingleton<IMongoDataLayerContext, MongoDataLayerContext>();
        builder.Services.AddSingleton<IUserDataLayer, UserDataLayer>();
        builder.Services.AddSingleton<IUserBusinessLayer, UserBusinessLayer>();
        builder.Services.AddSingleton<IMongoDbXmlKeyProtectorRepository, MongoDbXmlKeyProtectorRepository>();


        builder.Services.AddHostedService<StartupService>();
        builder.Services.AddHostedService<HostApplicationLifetimeEventsHostedService>();

        #endregion

        #region Caching

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddResponseCompression(options => {
                options.MimeTypes = new[]
                {
                    "text/html", "text/css"
                };
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
        }

        builder.Services.AddDistributedMemoryCache(options => {
            options.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddHybridCache(options => {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(30),
                LocalCacheExpiration = TimeSpan.FromSeconds(30),
                Flags = HybridCacheEntryFlags.None
            };
        });

        builder.Services.AddOutputCache(options => {
            options.AddBasePolicy(outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(TimeSpan.FromSeconds(10)));
            options.DefaultExpirationTimeSpan = OutputCachingPolicy.Expire30;

            options.AddPolicy(nameof(OutputCachingPolicy.Expire10), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire10));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire20), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire20));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire30), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire30));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire40), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire40));

            options.AddPolicy(nameof(OutputCachingPolicy.Expire50), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire50));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire60), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire60));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire120), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire120));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire240), build: outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire240));
        });
        builder.Services.AddResponseCaching();

        #endregion

        #region Cultures

        builder.Services.AddLocalization();
        builder.Services.Configure<RequestLocalizationOptions>(options => {
            var supportedCultures = AllowedCulture.SupportedCultures.ToArray();
            foreach (var culture in supportedCultures)
            {
                culture.NumberFormat = NumberFormatInfo.InvariantInfo;
                culture.DateTimeFormat = DateTimeFormatInfo.InvariantInfo;
            }
            options.SetDefaultCulture(supportedCultures[0].Name);
            options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.ApplyCurrentCultureToResponseHeaders = true;
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new CookieRequestCultureProvider
                {
                    CookieName = CookieNames.Culture,
                    Options = new RequestLocalizationOptions(),
                },
                new QueryStringRequestCultureProvider()
                {
                    QueryStringKey = CookieNames.Culture,
                    UIQueryStringKey = $"{CookieNames.Culture}-UI"
                },
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        #endregion

        #region Logging

        builder.Services.AddLogging();
        builder.Services.AddHttpLogging();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        #endregion

        #region Authenticate & Protection

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddSingleton<FailedLoginTracker>();
        builder.Services.AddSingleton<IJsonWebTokenCertificateProvider, JsonWebTokenCertificateProvider>();
        builder.Services.AddSingleton<RsaKeyProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorization(options => {
            options.AddPolicy(PolicyNamesAndRoles.Over18, configurePolicy: policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(18)));
            options.AddPolicy(PolicyNamesAndRoles.Over14, configurePolicy: policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(14)));
            options.AddPolicy(PolicyNamesAndRoles.Over7, configurePolicy: policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(7)));
        });
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.SlidingExpiration = true;
                options.LoginPath = PageRoutes.Account.SignIn;
                options.LogoutPath = PageRoutes.Account.Logout;
                options.AccessDeniedPath = PageRoutes.Account.Denied;
                options.ExpireTimeSpan = TimeSpan.FromHours(ProtectorTime.CookieExpireTimeSpan);
                options.Cookie = new CookieBuilder
                {
                    MaxAge = TimeSpan.FromHours(ProtectorTime.CookieMaxAge),
                    Name = CookieNames.AuthorizeCookie,
                    SameSite = SameSiteMode.Unspecified,
                    IsEssential = true,
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.Always,
                    Domain = CookieNames.Domain,
                    Path = "/"
                };
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ValidateAsync
                };

                #region Cookie Event Handler

                async Task ValidateAsync(CookieValidatePrincipalContext context)
                {
                    var userPrincipal = context.Principal;
                    if (userPrincipal == null)
                    {
                        await Reject();
                        return;
                    }

                    var authenticationType = userPrincipal.Identity?.AuthenticationType ?? string.Empty;
                    if (authenticationType == CookieNames.AuthenticationType)
                    {
                        // Example: Check if the user's security stamp is still valid
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<IUserBusinessLayer>();
                        var jswProvider = context.HttpContext.RequestServices.GetRequiredService<IJsonWebTokenCertificateProvider>();

                        var jwt = userPrincipal.FindFirst(ClaimTypes.UserData)?.Value;
                        if (!string.IsNullOrEmpty(jwt))
                        {
                            var claimsPrincipal = jswProvider.GetClaimsFromToken(jwt);
                            if (claimsPrincipal == null)
                            {
                                await Reject();
                                return;
                            }
                        }


                        var userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
                        var user = userId == null ? null : userManager.Get(userId);
                        if (user == null)
                        {
                            await Reject();
                        }
                    }
                    else
                    {
                        await Reject();
                    }
                    return;

                    async Task Reject()
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }

                #endregion

            });

        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromHours(ProtectorTime.SessionIdleTimeout);
            options.Cookie = new CookieBuilder
            {
                MaxAge = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                Name = CookieNames.Session,
                SameSite = SameSiteMode.Strict,
                Expiration = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                IsEssential = true,
                HttpOnly = true,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                Domain = CookieNames.Domain
            };
        });

        builder.Services.AddCors(options => {
            options.AddPolicy("AllowAllOrigins",
            policyBuilder => policyBuilder
                .WithOrigins("localhost:5217", "https://thnakdevserver.ddns.net:5001")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });

        builder.Services.ConfigureApplicationCookie(options => {
            options.Cookie.Name = CookieNames.AuthorizeCookie;
            options.Cookie.Domain = CookieNames.Domain;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        builder.Services.AddAntiforgery(options => {
            options.Cookie = new CookieBuilder
            {
                MaxAge = TimeSpan.FromHours(ProtectorTime.AntiforgeryCookieMaxAge),
                Name = CookieNames.Antiforgery,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                HttpOnly = true,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                Domain = CookieNames.Domain
            };
        });

        builder.Services.AddControllersWithViews(options => {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });


        builder.Services.AddDataProtection()
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
            })
            .SetApplicationName(CookieNames.Name)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
            .AddKeyManagementOptions(options => {
                options.AuthenticatedEncryptorConfiguration = new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                };
 #pragma warning disable ASP0000
                options.XmlRepository = builder.Services.BuildServiceProvider().GetService<IMongoDbXmlKeyProtectorRepository>();
 #pragma warning restore ASP0000
                options.AutoGenerateKeys = true;
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = null;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });


        builder.Services.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.FromMinutes(5); });

        #endregion

        #region Rate Limit

        builder.Services.AddRateLimiter(options => {
            options.AddFixedWindowLimiter(PolicyNamesAndRoles.LimitRate.Fixed, configureOptions: opt => {
                opt.Window = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 4;
                opt.QueueLimit = 2;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.RejectionStatusCode = 429;
        });
        builder.Services.AddRateLimiter(options => {
            options.AddSlidingWindowLimiter(PolicyNamesAndRoles.LimitRate.Sliding, configureOptions: opt => {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(30);
                opt.SegmentsPerWindow = 3;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            options.RejectionStatusCode = 429;
        });

        builder.Services.AddRateLimiter(options => {
            options.AddTokenBucketLimiter(PolicyNamesAndRoles.LimitRate.Token, configureOptions: opt => {
                opt.TokenLimit = 100;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                opt.TokensPerPeriod = 10;//Rate at which you want to fill
                opt.AutoReplenishment = true;
            });

            options.RejectionStatusCode = 429;
        });

        builder.Services.AddRateLimiter(options => {
            options.AddConcurrencyLimiter(PolicyNamesAndRoles.LimitRate.Concurrency, configureOptions: opt => {
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
                opt.PermitLimit = 100;
            });

            options.RejectionStatusCode = 429;
        });

        #endregion

        builder.Services.AddControllers();


        var app = builder.Build();

        #region Localization Setup

        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
        app.UseRequestLocalization(localizationOptions);

        #endregion

        app.UseCookiePolicy(new CookiePolicyOptions()
        {
            MinimumSameSitePolicy = SameSiteMode.Unspecified
        });

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseResponseCompression();
        }

        app.UseCors("AllowAllOrigins");
        app.UseRateLimiter();

        app.UseResponseCaching();
        app.UseOutputCache();

        app.UseSession();

        app.UseStaticFiles(new StaticFileOptions
        {
            // OnPrepareResponse = (context) => {
            //     ApplyHeaders(context.Context.Response.Headers);
            // }
        });
        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(_Imports).Assembly);
        //app.Use(async (context, next) => {
        //    ApplyHeaders(context.Response.Headers);
        //    await next();
        //});

        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.Run();
    }
    private static void ApplyHeaders(IHeaderDictionary headers)
    {
        headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    }
}