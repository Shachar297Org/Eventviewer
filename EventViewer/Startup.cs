using EventViewer.ApiClients;
using EventViewer.Data;
using EventViewer.Interfaces;
using EventViewer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventViewer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<LiteDbOptions>(Configuration.GetSection("LiteDbOptions"));
            services.AddSingleton<ILiteDbContext, LiteDbContext>();
            services.AddTransient<ILiteDBEventsDataService, LiteDBEventsDataService>();

            services.AddTransient<IEventsDataService, EventsDataService>();

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(int.Parse(Configuration["LumX:SessionIdleTimeout"]));
                // You might want to only set the application cookies over a secure connection:
                //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
                
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddHttpClient<IUserApiAuthenticationService, UserApiAuthenticationService>();
                        
            // The DelegatingHandler has to be registered as a Transient Service
            services.AddTransient<ProtectedApiBearerTokenHandler>();

            services.AddHttpClient<IEventsApiClient, EventsApiClient>(client =>
            {
                client.BaseAddress = new Uri($"https://api.{Configuration["LumX:Environment"]}.lumenisx.lumenis.com");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(300);
            }).AddHttpMessageHandler<ProtectedApiBearerTokenHandler>();

            services.AddTransient<IEventsDataService, EventsDataService>();

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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseSession();
            /*app.Run(async (context) =>
            {
                if (context.Session.Keys.Contains("name"))
                {
                    //await context.Response.WriteAsync($"Hello {context.Session.GetString("name")}!");
                }
                else
                {
                    //context.Session.SetString("name", "Tom");
                    //await context.Response.WriteAsync("Hello World!");
                }
            });*/

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=EventViewer}/{action=Index}/{id?}");
            });

        }
    }
}
