using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MyThuatShop.Services;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// HttpClientFactory
builder.Services.AddHttpClient();

// ===== Session =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MyThuatShop.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== API BaseUrl =====
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
    apiBaseUrl = "https://localhost:7090";

// handler bỏ qua SSL (dev)
HttpMessageHandler CreateHandler()
{
    var h = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        h.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return h;
}

// ===== Services dùng tự tạo HttpClient trong hàm -> AddScoped =====
builder.Services.AddHttpClient<HomeApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl!);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddScoped<ProductAPIService>();
builder.Services.AddScoped<OrderApiService>();

builder.Services.AddSingleton<IVnPayService, VnPayService>();

builder.Services.AddHttpClient<SearchApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl!);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);

// Dùng 1 typed client duy nhất cho AccountApiService
builder.Services.AddHttpClient<AccountApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["ApiBaseUrl"]!);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var handler = new HttpClientHandler();

    if (env.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

// GHN Service
builder.Services.AddHttpClient<GhnService>((sp, client) =>
{
    // TODO: set BaseAddress/headers nếu cần
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var handler = new HttpClientHandler();

    if (env.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

builder.Services.AddHttpClient<AdminOverviewApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddHttpClient<AdminStatisticsApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);

// ✅ THÊM Category typed client (bắt buộc)
builder.Services.AddHttpClient<AdminCategoryApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);




// ===== AUTH =====
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
bool hasGoogle = !string.IsNullOrWhiteSpace(googleClientId) &&
                 !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = hasGoogle ? "Google" : "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
})
.AddCookie("External", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

if (hasGoogle)
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SignInScheme = "External";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
}

builder.Services.AddHttpClient<ContactApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl!);
}).ConfigurePrimaryHttpMessageHandler(CreateHandler);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Session trước Auth để đọc session trong layout/admin
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
