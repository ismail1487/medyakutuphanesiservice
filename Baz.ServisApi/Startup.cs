using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Decor;
using Microsoft.AspNetCore.Http;
using Baz.AOP.Logger.ExceptionLog;
using Baz.AOP.Logger.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Baz.ProcessResult;
using Baz.RequestManager.Abstracts;
using Baz.RequestManager;
using Baz.Model.Pattern;
using Baz.SharedSession;
using Baz.Service;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using Baz.AletKutusu;
using Baz.Model.Entity.Constants;
using System.Configuration;

namespace Baz.MedyaServiceApi
{
    /// <summary>
    /// Uygulamayı ayağa kaldıran class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        /// <summary>
        /// Uygulamayı ayağa kaldıran servisin yapıcı methodudur.
        /// </summary>
        /// <param name="env"></param>
        public Startup(IWebHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables().Build();
        }

        /// <summary>
        /// Uygulamayı yapılandıran özellik
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            SetCoreURL(Configuration);

            services.AddHttpContextAccessor();
            //services.AddControllers(c => { c.Filters.Add(typeof(ModelValidationFilter), int.MinValue) });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Baz.MedyaServiceApi", Version = "v1" });
                c.OperationFilter<DefaultHeaderParameter>();
            });

            services.AddDbContext<Repository.Pattern.IDataContext, Repository.Pattern.Entity.DataContext>(conf => conf.UseSqlServer(Configuration.GetConnectionString("Connection")));
            services.AddSingleton<Baz.Mapper.Pattern.IDataMapper>(new Baz.Mapper.Pattern.Entity.DataMapper(GenerateConfiguratedMapper()));
            //////////////////////////////////////////SESSION SERVER AYARLARI/////////////////////////////////////////////////
            //Distributed session iþlemleri için session serverýn network baðlantýlarýný yapýlandýrýr.
            services.AddDistributedSqlServerCache(p =>
            {
                p.ConnectionString = Configuration.GetConnectionString("SessionConnection");
                p.SchemaName = "dbo";
                p.TableName = "SQLSessions";
            });
            services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.Path = "/";
                options.Cookie.Name = "Test.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(60);
            });
            services.AddSession();
            //Http desteği olmadan paylaşımlı session işlemleri yapan servisi kayıt eder.
            services.AddTransient<Baz.SharedSession.ISharedSession, Baz.SharedSession.BaseSharedSession>();
            //Http desteği olan işlemler için paylaşımlı session nesnesinin kaydını yapar.
            //BaseSharedSessionForHttpRequest işlemleri için öncelikle BaseSharedSession servisi kayıt edilmelidir.
            services.AddTransient<Baz.SharedSession.ISharedSessionForHttpRequest, Baz.SharedSession.BaseSharedSessionForHttpRequest>();
            //////////////////////////////////////////////////////////////////////////////////////

            services.AddScoped<Repository.Pattern.IUnitOfWork, Repository.Pattern.Entity.UnitOfWork>();
            services.AddScoped(typeof(Repository.Pattern.IRepository<>), typeof(Repository.Pattern.Entity.Repository<>));
            services.AddScoped(typeof(Service.Base.IService<>), typeof(Service.Base.Service<>));
            services.AddScoped<ILoginUser, LoginUserManager>();
            services.AddScoped<IMedyaKutuphanesiService, MedyaKutuphanesiService>();
            services.AddScoped<IParamMedyaTipleriService, ParamMedyaTipleriService>();

            services.AddTransient<IRequestHelper, RequestHelper>(provider =>
            {
                return new RequestHelper("", new RequestManagerHeaderHelperForHttp(provider).SetDefaultHeader());
            });
            var types = typeof(Service.Base.IService<>).Assembly.GetTypes();
            var interfaces = types.Where(p => p.IsInterface && p.GetInterface("IService`1") != null).ToList();

            //Exception loglarýný iþleyen Baz.AOP.Logger.ExceptionLog servisinin kaydýný yapar
            services.AddAOPExceptionLogging();
            //Http iþlemleri için loglama yapan BaseHttpLogger servisinin kaydýný yapar.

            foreach (var item in interfaces)
            {
                var serviceTypes = types.Where(p => p.GetInterface(item.Name) != null && !p.IsInterface).ToList();
                serviceTypes.ForEach(p => services.AddScoped(item, p).Decorated());
            }
            services.AddResponseCompression();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(
                            Configuration.GetValue<string>("WebAppLive"),
                            Configuration.GetValue<string>("SocketLive"),
                            Configuration.GetValue<string>("MedyaKutuphanesiLive"),
                            Configuration.GetValue<string>("KisiServis"),
                            Configuration.GetValue<string>("KurumServis"),
                            Configuration.GetValue<string>("WebAppLocal"),
                            Configuration.GetValue<string>("MedyaIP")
                        )
                        .AllowAnyMethod()
                        .SetIsOriginAllowed((x) => true)
                        .AllowCredentials();
                });
            });
        }

        /// <summary>
        ///Bu yöntem çalışma zamanı tarafından çağrılır. HTTP istek ardışık düzenini yapılandırmak için bu yöntemi kullanın.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="lifetime"></param>
        /// <param name="cache"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
            IDistributedCache cache)
        {
            // Configure the Localization middleware
            app.UseRequestLocalization();
            ////////////////////////////////// SESSION SERVER AYARLARI/////////////////////////////////////
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseSession();
            lifetime.ApplicationStarted.Register(() =>
            {
                var currentTimeUTC = DateTime.UtcNow.ToString();
                byte[] encodedCurrentTimeUTC = Encoding.UTF8.GetBytes(currentTimeUTC);
                var options = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(20));
                cache.Set("cachedTimeUTC", encodedCurrentTimeUTC, options);
            });
            /////////////////////////////////////////////////////////////////////////////////////

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
                c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "Baz.MedyaKütüphanesi v1");
            });

            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "MedyaKutuphanesi"));

            //app.UseFileServer(new FileServerOptions()
            //{FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "MedyaKutuphanesi")),
            //    RequestPath = "/MedyaKutuphanesi", })
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "MedyaKutuphanesi")),
                RequestPath = "/MedyaKutuphanesi",
                OnPrepareResponse = (context) =>
                {
                    //if (!context.Context.User.Identity.IsAuthenticated)
                    //{ context.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized
                    //    context.Context.Response.ContentLength = 0
                    //    context.Context.Response.Body = Stream.Null //}
                }
            });

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseMiddleware<AuthMiddleware>();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseCors();
        }

        private static Profile GenerateConfiguratedMapper()
        {
            var mapper = Baz.Mapper.Pattern.Entity.DataMapperProfile.GenerateProfile();

            return mapper;
        }

        private static void SetCoreURL(IConfiguration configuration)
        {
            Model.Entity.Constants.LocalPortlar.CoreUrl = configuration.GetValue<string>("CoreUrl");

            var section = configuration.GetSection("LocalPortlar");
            LocalPortlar.WebApp = section.GetValue<string>("WebApp");
            LocalPortlar.UserLoginregisterService = section.GetValue<string>("UserLoginregisterService");
            LocalPortlar.KisiServis = section.GetValue<string>("KisiServis");
            LocalPortlar.MedyaKutuphanesiService = section.GetValue<string>("MedyaKutuphanesiService");
            LocalPortlar.IYSService = section.GetValue<string>("IYSService");
            LocalPortlar.KurumService = section.GetValue<string>("KurumService");
        }
    }
}