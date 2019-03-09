using System.IO;
using System.Linq;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfDotNet.IS4Host
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", false)
                .Build();

            var connectionString = config.GetSection("connectionString").Value;

            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                //.AddInMemoryIdentityResources(Config.GetIdentityResources())
                //.AddInMemoryApiResources(Config.GetApiResources())
                //.AddInMemoryClients(Config.GetClients())
                .AddTestUsers(Config.GetUsers())
                // ConfigurationStore Store: clients and resources
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(connectionString, sql =>
                            sql.MigrationsAssembly(migrationAssembly));
                })
                // Operational Store: tokens, consents, codes, etc.
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(connectionString,
                            sql => sql.MigrationsAssembly(migrationAssembly));
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //InitializeIdentityServerDatabase(app);

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private void InitializeIdentityServerDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app
                .ApplicationServices
                .GetService<IServiceScopeFactory>()
                .CreateScope())
            {
                //serviceScope
                //    .ServiceProvider.GetRequiredService<PersistedGrantDbContext>()
                //    .Database
                //    .Migrate();

                var context = serviceScope
                    .ServiceProvider
                    .GetRequiredService<ConfigurationDbContext>();

                //context.Database.Migrate();

                // Seed the Data
                if (context.Clients.Any())
                {
                    foreach (var client in Config
                        .GetClients()
                        .Where(c => c.ClientId == "swaggerapiui"))
                        context.Clients.Add(client.ToEntity());

                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var identityResource in Config.GetIdentityResources())
                        context.IdentityResources.Add(identityResource.ToEntity());

                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var apiResource in Config.GetApiResources())
                    {
                        context.ApiResources.Add(apiResource.ToEntity());
                    }

                    context.SaveChanges();
                }
            }
        }
    }
}