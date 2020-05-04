using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EmployeeManagement
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<AppDbContext>(
                options => options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 3;

                options.SignIn.RequireConfirmedEmail = true;

                options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<CustomEmailConfirmationTokenProvider
                <ApplicationUser>>("CustomEmailConfirmation");

            services.Configure<DataProtectionTokenProviderOptions>(o =>
                        o.TokenLifespan = TimeSpan.FromHours(5));

            services.Configure<CustomEmailConfirmationTokenProviderOptions>(o =>
                        o.TokenLifespan = TimeSpan.FromDays(3));

            //services.addControllerWithViews(o=>o.Filters.Add(new AuthorizeFilter())); 3.0 services.AddRazorPages();/
            //services.AddAuthentication(CookieAuthenticationDefaults)
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddXmlSerializerFormatters();

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = "803402001432-f008lgc3l61aga7socd4a5akrfm3irk5.apps.googleusercontent.com";/*_config["Google:ClientID"] */
                    options.ClientSecret = "exiXHhWW2rPRXoFAHgC88jil"; /*_config["Google:ClientSecret"]; */
                })
                .AddFacebook(options =>
                {
                    options.AppId = "652154675325284";
                    options.AppSecret = "ea7d27001e1bc42cab8f5404aec39479";
                });

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy",
                    policy => policy.RequireClaim("Delete Role"));

                options.AddPolicy("EditRolePolicy",
                    policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));
                //options.InvokeHandlersAfterFailure = false; default true
                options.AddPolicy("AdminRolePolicy",
                    policy => policy.RequireRole("Admin"));
            });


            ////AntiforgeryToken
            ////specify options for the anti forgery here
            //for newwer version
            //////services.AddAntiforgery(opts => { opts.Cookie.SecurePolicy = new CookieSecurePolicy(); });
            // services.AddAntiforgery(opts => { opts.RequireSsl = true; });

            ////anti forgery as global filter
            //services.AddMvc(options =>
            //    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));



            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();

            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
            services.AddSingleton<DataProtectionPurposeStrings>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }
            //app.useHttpsRedirection(); 3.0 for change http to https
            app.UseStaticFiles();
            app.UseAuthentication();
            /*
            app.useRouting();
            app.useEndPoints(endpoint=>
            {
                 endpoint.MapControllerRoute(name :default,Pattern:"{controller=home}/{action=index}/{id?});
                 endpoint.MapRazorPages();
            });
            */            
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
