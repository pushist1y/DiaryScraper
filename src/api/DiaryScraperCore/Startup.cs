using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Web;

namespace DiaryScraperCore
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
            services.AddMvc();
            services.AddSingleton<ScrapeTaskRunner>();
            services.AddSingleton<ParseTaskRunner>();
            services.AddTransient<DiaryScraperFactory>();
            services.AddTransient<DiaryParserFactory>();
            services.AddDbContext<ScrapeContext>((options) => {
                options.UseSqlite("Data Source=scrape.db");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            loggerFactory.AddConsole();
            loggerFactory.AddDebug();
            loggerFactory.AddNLog();

            app.AddNLogWeb();

            if(NLog.LogManager.Configuration == null){
                 NLog.LogManager.Configuration= new LoggingConfiguration();
            }
        }
    }
}
