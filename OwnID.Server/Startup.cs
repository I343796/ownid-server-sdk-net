using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OwnID.Extensibility.Configuration;
using OwnID.Integrations.Firebase;
using OwnID.Redis;
using OwnID.Server.Middlewares.SecurityHeaders;
using OwnID.Web;
using OwnID.Integrations.Gigya;
using OwnID.Server.Metrics;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using ConfigInjectionMiddleware = OwnID.Server.Middlewares.ConfigInjectionMiddleware;
using SecurityHeadersBuilder = OwnID.Server.Middlewares.SecurityHeaders.SecurityHeadersBuilder;
using TelemetryLogService = OwnID.Server.Metrics.TelemetryLogService;

namespace OwnID.Server
{
    public class Startup
    {
        private const string CorsPolicyName = "AllowAll";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigureLogging();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var ownIdSection = Configuration.GetSection("ownid");

            if (!Enum.TryParse(Configuration["server_mode"], true, out ServerMode serverMode))
                serverMode = ServerMode.Production;

            var topDomain = ownIdSection["top_domain"];
            var webAppUrl = new Uri(ownIdSection["web_app_url"] ?? Constants.OwinIdApplicationAddress);

            services.AddCors(x =>
            {
                x.AddPolicy(CorsPolicyName, builder =>
                {
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowCredentials();
                    builder.SetIsOriginAllowedToAllowWildcardSubdomains();

                    var originsList = new List<string>();

                    switch (serverMode)
                    {
                        case ServerMode.Pilot:
                            builder.SetIsOriginAllowed(_ => true);
                            break;
                        case ServerMode.Local:
                            builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");
                            break;
                        default:
                            originsList.Add($"https://*.{topDomain}");
                            originsList.Add($"https://{topDomain}");
                            originsList.Add(webAppUrl.ToString().TrimEnd('/'));

                            var additionalOrigins = ownIdSection["add_cors_origins"];

                            if (!string.IsNullOrWhiteSpace(additionalOrigins))
                                originsList.AddRange(additionalOrigins.Split(';').Select(o => o.Trim()));
                            break;
                    }

                    builder.WithOrigins(originsList.ToArray());
                });
            });

            services.AddMetrics(Configuration);

            services.AddOwnId(builder =>
            {
                switch (ownIdSection.GetValue("integration", "gigya"))
                {
                    case "firebase":
                        var firebaseSection = Configuration.GetSection("firebase");
                        builder.UseFirebase(firebaseSection["credentials_json"]);
                        break;
                    default:
                        var gigyaSection = Configuration.GetSection("gigya");
                        var loginTypeString = gigyaSection["login_type"];

                        if (string.IsNullOrEmpty(loginTypeString) || !Enum.TryParse(gigyaSection["login_type"], true,
                            out GigyaLoginType loginType)) 
                            loginType = GigyaLoginType.Session;
                        builder.UseGigya(gigyaSection["data_center"], gigyaSection["api_key"], gigyaSection["secret"],
                            gigyaSection["user_key"], loginType);
                        break;
                }

                builder.SetKeys(ownIdSection["pub_key"], ownIdSection["private_key"]);

                switch (ownIdSection["cache_type"])
                {
                    case "web-cache":
                        builder.UseWebCacheStore();
                        break;
                    case "redis":
                        builder.UseCacheStore(ServiceLifetime.Singleton, provider =>
                        {
                            var configuration = provider.GetService<IOwnIdCoreConfiguration>();
                            var logger = provider.GetService<ILogger<RedisCacheStore>>();
                            return new RedisCacheStore(Configuration.GetSection("ownid")?["cache_config"],
                                configuration, logger);
                        });
                        break;
                }

                builder.WithBaseSettings(x =>
                {
                    x.DID = ownIdSection["did"];
                    x.Name = ownIdSection["name"];
                    x.Description = ownIdSection["description"];
                    x.Icon = ownIdSection["icon"];
                    var logoUrl = ownIdSection["logo"];
                    x.LogoUrl = !string.IsNullOrEmpty(logoUrl) ? new Uri(logoUrl) : null;
                    x.CallbackUrl = new Uri(ownIdSection["callback_url"]);
                    x.TopDomain = topDomain;

                    x.CacheExpirationTimeout = ownIdSection.GetValue("cache_expiration",
                        (uint) TimeSpan.FromMinutes(10).TotalMilliseconds);

                    if (ownIdSection.Key.Contains("maximum_number_of_connected_devices"))
                        x.MaximumNumberOfConnectedDevices =
                            ownIdSection.GetValue<uint>("maximum_number_of_connected_devices");

                    x.OwnIdApplicationUrl = webAppUrl;
                    x.OverwriteFields = ownIdSection.GetValue<bool>("overwrite_fields");

                    x.TFAEnabled = ownIdSection.GetValue("tfa_enabled", true);
                    x.Fido2FallbackBehavior =
                        ownIdSection.GetValue("fido2_fallback_behavior", Fido2FallbackBehavior.Passcode);

                    // FIDO2 configuration
                    if (!string.IsNullOrWhiteSpace(ownIdSection["fido2_passwordless_page_url"]))
                        x.Fido2.PasswordlessPageUrl = new Uri(ownIdSection["fido2_passwordless_page_url"]);

                    x.Fido2.RelyingPartyId = ownIdSection["fido2_relying_party_id"];
                    x.Fido2.RelyingPartyName = ownIdSection["fido2_relying_party_name"];
                    x.Fido2.UserName = ownIdSection["fido2_user_name"];
                    x.Fido2.UserDisplayName = ownIdSection["fido2_user_display_name"];

                    if (!string.IsNullOrWhiteSpace(ownIdSection["fido2_origin"]))
                        x.Fido2.Origin = new Uri(ownIdSection["fido2_origin"]);

                    x.LogLevel = Configuration.GetValue("Serilog:MinimumLevel:Default", LogLevel.Information);

                    //for development cases
                    x.IsDevEnvironment = serverMode == ServerMode.Local;
                });

                var smtpSection = Configuration.GetSection("smtp");

                if (smtpSection.Exists())
                    builder.UseSmtp(smtp =>
                    {
                        smtp.FromAddress = smtpSection["from_address"];
                        smtp.FromName = smtpSection["from_name"];
                        smtp.UserName = smtpSection["user_name"];
                        smtp.Password = smtpSection["password"];
                        smtp.Host = smtpSection["host"];
                        smtp.UseSsl = smtpSection.GetValue("ssl", false);
                        smtp.Port = smtpSection.GetValue("port", 0);
                    });

                var magicLinkSection = ownIdSection.GetSection("magic_link");

                if (magicLinkSection.Exists() && magicLinkSection.GetValue("enabled", false))
                    builder.UseMagicLink(ml =>
                    {
                        ml.RedirectUrl = new Uri(magicLinkSection["redirect_url"]);
                        ml.TokenLifetime = magicLinkSection.GetValue<uint>("token_lifetime", 0);
                        ml.SameBrowserUsageOnly = magicLinkSection.GetValue("same_browser", true);
                    });
            });

            // on log level info
            services.AddHostedService<TelemetryLogService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRequestLocalization(x =>
            {
                var supportedCultures = new[]
                {
                    "ar", "de", "en", "en-GB", "es", "es-MX", "fr", "id", "ja", "ko", "ms", "pt", "pt-PT", "ru", "th",
                    "tr", "vi", "zh-CN", "zh-TW"
                };

                x.AddSupportedCultures(supportedCultures);
                x.DefaultRequestCulture = new RequestCulture("en", "en");
                x.AddSupportedUICultures(supportedCultures);
            });
            app.UseCors(CorsPolicyName);

            app.UseSecurityHeadersMiddleware(new SecurityHeadersBuilder()
                .AddStrictTransportSecurityMaxAgeIncludeSubDomains()
                .AddContentTypeOptionsNoSniff());

            var logger = app.ApplicationServices.GetService<ILogger<LogRequestMiddleware>>();
            if (logger?.IsEnabled(LogLevel.Debug) ?? false)
                app.UseMiddleware<LogRequestMiddleware>();

            app.UseMetrics();
            app.UseOwnId();

            var routeBuilder = new RouteBuilder(app);
            routeBuilder.MapMiddlewarePost("ownid/log",
                builder => builder.UseMiddleware<LogMiddleware>());

            var aspEnv = Configuration.GetValue("ASPNETCORE_ENVIRONMENT", string.Empty);
            if (aspEnv == "dev" || aspEnv == "staging")
                routeBuilder.MapMiddlewarePut("/ownid/config-injection",
                    builder => builder.UseMiddleware<ConfigInjectionMiddleware>());

            app.UseRouter(routeBuilder.Build());
        }

        private void ConfigureLogging()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var elasticSection = Configuration.GetSection("ElasticConfiguration");
            var elasticLoggingEnabled = Convert.ToBoolean(elasticSection["Enabled"]);

            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Debug()
                .WriteTo.Console();

            if (elasticLoggingEnabled)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                logger.WriteTo.Elasticsearch(ConfigureElasticSink(elasticSection, environment))
                    .Enrich.WithProperty("source", "net-core-3-sdk")
                    .Enrich.WithProperty("version", version);
            }

            Log.Logger = logger.Enrich.WithProperty("environment", environment)
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        private ElasticsearchSinkOptions ConfigureElasticSink(IConfigurationSection configuration, string environment)
        {
            var env = (environment ?? "development").ToLower().Replace(".", "-");

            return new ElasticsearchSinkOptions(new Uri(configuration["Uri"]))
            {
                AutoRegisterTemplate = true,
                IndexDecider = (ev, offset) =>
                {
                    var sunday = DateTime.UtcNow.Date.AddDays(0 - (int) DateTime.UtcNow.DayOfWeek);
                    var saturday = sunday.AddDays(6);
                    return $"ownid-{env}-{sunday:yyyy-MM-dd}-{saturday:yyyy-MM-dd}";
                },
                ModifyConnectionSettings = x =>
                    x.BasicAuthentication(configuration["Username"], configuration["Password"]),
                InlineFields = true,
                OverwriteTemplate = true,
                CustomFormatter = new OwnIdFormatter()
            };
        }
    }
}