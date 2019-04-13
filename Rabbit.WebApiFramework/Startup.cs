using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Rabbit.WebApiFramework.Core;
using Rabbit.WebApiFramework.Core.Interface;

namespace Rabbit.WebApiFramework
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            
            var assemblyLst = LoadPlugins();

            services.Configure<RazorViewEngineOptions>(options =>
            {
                foreach (var ass in assemblyLst)
                {
                    options.FileProviders.Add(new EmbeddedFileProvider(ass));
                }
            });
            services.AddMvc().ConfigureApplicationPartManager(manager =>
            {
                foreach (var ass in assemblyLst)
                {
                    manager.ApplicationParts.Add(new AssemblyPart(ass));
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        ///  加载自定的扩展
        /// </summary>
        /// <returns>List&lt;Assembly&gt;.</returns>
        protected List<Assembly> LoadPlugins()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            var pConfig = Configuration.GetSection("Plugins").Get<PluginConfiguraton>();
            var lst = new List<Assembly>();
            foreach (var plugin in pConfig.Path)
            {
                var file = Path.Combine(Environment.CurrentDirectory, "Plugins", plugin);
                if (!File.Exists(file)) continue;
                var assembly = Assembly.LoadFile(file);
                lst.Add(assembly);
            }
            return lst;
        }
        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = args.RequestingAssembly;
            var assemblyName = new AssemblyName(args.Name);
            var location = args.RequestingAssembly.Location;
            if (File.Exists(location))
            {
                assembly = Assembly.LoadFile(location);
            }
            else
            {
                var file = Path.Combine(Path.GetDirectoryName(location), $"{assemblyName.Name}.dll");
                if (File.Exists(file))
                {
                    assembly = Assembly.LoadFile(file);
                }
            }
            return assembly;
        }
    }
    public class PluginConfiguraton
    {
        public string[] Path { get; set; } = { };
    }
}
