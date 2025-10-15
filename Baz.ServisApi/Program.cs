using Baz.AOP.Logger.ExceptionLog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Baz.MedyaServiceApi
{
    /// <summary>
    /// API'ın çalışması için gereken Main() methodunu barındıran class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        /// <summary>
        /// API'ı ayağa kaldıran method.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Host oluşturan method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("https://*:51305");
                }).BazConfigureLogging(); // Graylog a log yazýlabilmesi için Baz.AOP.Logger.ExceptionLog paketi eklenip BazConfigureLogging() fonksiyonu çaðrýlýr.

        // BazConfigureLogging() fonksiyonu graylog için gerekli netwok ayarlarýný yapar. network ayarlarýný appsetting.json dan alýr.
    }
}