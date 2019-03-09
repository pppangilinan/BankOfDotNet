using System.Collections.Generic;
using System.Linq;
using BankOfDotNet.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BankOfDotNet.Api
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
            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = "http://localhost:6576/";
                    options.RequireHttpsMetadata = false;
                    options.ApiName = "bankOfDotNetApi";
                });

            services.AddDbContext<BankContext>(options => options.UseInMemoryDatabase("BankingDb"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Title = "BankOfDotNet Api",
                    Version = "v1"
                });

                options.OperationFilter<CheckAuthorizeOperationFilter>();

                options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = "http://localhost:6576/connect/authorize",
                    TokenUrl = "http://localhost:6576/connect/token",
                    Scopes = new Dictionary<string, string>
                    {
                        {
                            "bankOfDotNetApi",
                            "Customer Api for BankOfDotNet"
                        }
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseAuthentication();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "BankOfDotNet Api V1");
                options.OAuthClientId("swaggerapiui");
                options.OAuthAppName("Swagger API UI");
            });
        }
    }

    internal class CheckAuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // Check for any existing Authorize Attribute
            var authorizeAttributeExists =
                context
                    .ApiDescription
                    .ControllerAttributes()
                    .OfType<AuthorizeAttribute>()
                    .Any() || context
                    .ApiDescription
                    .ActionAttributes()
                    .OfType<AuthorizeAttribute>()
                    .Any();

            if (authorizeAttributeExists)
            {
                operation.Responses.Add("401", new Response
                {
                    Description = "Unauthorized"
                });
                operation.Responses.Add("403", new Response
                {
                    Description = "Forbidden"
                });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(new Dictionary<string, IEnumerable<string>>
                {
                    {"oauth2", new[] {"bankOfDotNetApi"}}
                });
            }
        }
    }
}