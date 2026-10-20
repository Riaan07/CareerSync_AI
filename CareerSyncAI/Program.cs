using System.Security.Claims;
using CareerSyncAI.Data;
using CareerSyncAI.Hubs;
using CareerSyncAI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Render and other hosting platforms
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<CareerSyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();

builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddSingleton<ICvTextExtractor, CvTextExtractor>();
builder.Services.AddScoped<IAdminReportBuilder, AdminReportBuilder>();
builder.Services.AddMemoryCache();

var twilioOptions = builder.Configuration.GetSection(TwilioOptions.SectionName).Get<TwilioOptions>() ?? new TwilioOptions();
builder.Services.AddSingleton(twilioOptions);
if (twilioOptions.IsConfigured)
    builder.Services.AddHttpClient<ISmsVerificationService, TwilioVerifyService>(http => http.Timeout = TimeSpan.FromSeconds(20));
else
    builder.Services.AddSingleton<ISmsVerificationService, DevelopmentSmsVerificationService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var repository = context.HttpContext.RequestServices.GetRequiredService<IRepositoryWrapper>();

                if (!int.TryParse(idClaim, out var userId) || !repository.AppUser.IsActiveUser(userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            },
        };
    });

var app = builder.Build();

if (!twilioOptions.IsConfigured)
    app.Logger.LogWarning("Twilio is not configured — password-reset codes will be shown in this console and saved to {Folder} instead of being texted.",
        Path.Combine(app.Environment.ContentRootPath, "App_Data", "DevSms"));


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CareerSyncDbContext>();
    db.Database.Migrate();

    var repositoryWrapper = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
    SeedIdentity.Seed(repositoryWrapper);
    SeedSkillDictionary.Seed(repositoryWrapper);
    SeedLearningResources.Seed(repositoryWrapper);
    SeedDemoData.Seed(repositoryWrapper);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
