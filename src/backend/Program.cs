
using System.Net;

using DocAssistant.Charty.Ai;

using NLog;
using NLog.Web;

namespace MinimalApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LogManager.Setup().LoadConfigurationFromAppSettings();
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                HttpClient.DefaultProxy = new WebProxy()
                {
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = true
                };

                var builder = WebApplication.CreateBuilder(args);
                //builder.Configuration.ConfigureAzureKeyVault();  

                // See: https://aka.ms/aspnetcore/swashbuckle  
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddOutputCache();
                builder.Services.AddControllersWithViews();
                builder.Services.AddRazorPages();
                builder.Services.AddCrossOriginResourceSharing();  
                builder.Services.AddAzureServices();
                //builder.Services.AddAntiforgery(options =>  
                //{  
                //    options.HeaderName = "X-CSRF-TOKEN-HEADER";  
                //    options.FormFieldName = "X-CSRF-TOKEN-FORM";  
                //});  
                builder.Services.AddHttpClient();

                //if (builder.Environment.IsDevelopment())
                //{
                //    builder.Services.AddDistributedMemoryCache();
                //}
                //else
                //{
                //    //static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);  

                //    // Set application telemetry  
                //    // if (GetEnvVar("APPLICATIONINSIGHTS_CONNECTION_STRING") is string appInsightsConnectionString && !string.IsNullOrEmpty(appInsightsConnectionString))  
                //    // {  
                //    //     builder.Services.AddApplicationInsightsTelemetry(option =>  
                //    //     {  
                //    //         option.ConnectionString = appInsightsConnectionString;  
                //    //     });  
                //    // }  
                //}

                builder.Services.AddAiServices();

                //builder.Services.AddCors(options =>  
                //{  
                //    options.AddPolicy("AllowAllOrigins",  
                //        builder => builder.AllowAnyOrigin()  
                //            .AllowAnyHeader()  
                //            .AllowAnyMethod());  
                //});  

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    app.UseWebAssemblyDebugging();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseOutputCache();
                app.UseRouting();
                app.UseStaticFiles();
                app.UseCors();
                app.UseBlazorFrameworkFiles();
                //app.UseAntiforgery();  

                app.MapRazorPages();
                app.MapControllers();

                //app.Use(next => context =>  
                //{  
                //    var antiforgery = app.Services.GetRequiredService<IAntiforgery>();  
                //    var tokens = antiforgery.GetAndStoreTokens(context);  
                //    context.Response.Cookies.Append("XSRF-TOKEN", tokens?.RequestToken ?? string.Empty, new CookieOptions { HttpOnly = false });  
                //    return next(context);  
                //});

                app.MapFallbackToFile("index.html");
                app.MapApi();

                app.Run();
            }
            catch (Exception ex)
            {
                // NLog: catch setup errors  
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)  
                LogManager.Shutdown();
            }
        }
    }
}
