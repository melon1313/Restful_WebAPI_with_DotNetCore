using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fake.API.Database;
using Fake.API.Models;
using Fake.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;

namespace Fake.API
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var secretByte = Encoding.UTF8.GetBytes(Configuration["Authentication:SecretKey"]);
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Authentication:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = Configuration["Authentication:Audience"],

                        ValidateLifetime = true,

                        IssuerSigningKey = new SymmetricSecurityKey(secretByte)
                    };
                }
                );

            //加入MVC架構中的控制器服務
            //加入Web API的內容協商
            services.AddControllers(
                    //只允許回傳已設定好的格式
                    setUpAction => { setUpAction.ReturnHttpNotAcceptable = true; }
                )
                //JsonPatch 允許序列化
                .AddNewtonsoftJson(setupAction => { setupAction.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); })
                //允許回傳XML格式
                .AddXmlDataContractSerializerFormatters();

            services.AddTransient<ITouristRouteRepository, TouristRouteRepository>();
            services.AddDbContext<AppDbContext>(option =>
            {
                option.UseSqlServer(Configuration["DbContext:ConnectionString"]);
            });

            services.AddAutoMapper(typeof(Startup));

            //註冊http請求服務
            services.AddHttpClient();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.Configure<MvcOptions>(
                config =>
                {
                    var outputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();

                    if(outputFormatter != null)
                    {
                        outputFormatter.SupportedMediaTypes.Add("application/vnd.alice.hateoas+json");
                    }
                }    
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //你在哪?
            app.UseRouting();

            //你是誰?
            app.UseAuthentication();

            //你可以幹什麼? 有什麼權限?
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllers();

                //endpoints.MapGet("/test", async context => 
                //{
                //    await context.Response.WriteAsync("Hello Test!"); 
                //    //throw new Exception("test");

                //});

                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync("Hello World!");
                //});
            });
        }
    }
}
