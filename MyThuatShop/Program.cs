using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MyThuatShop.Services;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// HttpClient
builder.Services.AddHttpClient();

// DI services
builder.Services.AddScoped<HomeApiService>();
builder.Services.AddScoped<ProductAPIService>();
builder.Services.AddScoped<OrderApiService>();
builder.Services.AddHttpClient<AccountApiService>();
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddHttpClient<AccountApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["ApiBaseUrl"]!);
});


builder.Services.AddHttpClient<SearchApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["ApiBaseUrl"]!);
});

builder.Services.AddHttpContextAccessor();

// ✅ Session (CHỈ 1 LẦN)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MyThuatShop.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30); // giống JSP: đủ lâu để test
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// ✅ đọc config Google và kiểm tra
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

//if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
//{
//    throw new Exception("Thiếu cấu hình Google OAuth. Kiểm tra appsettings.json: Authentication:Google:ClientId và ClientSecret");
//}
// ✅ AUTH: Cookies + External + Google
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie("Cookies")
    .AddCookie("External");


if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle("Google", options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = "External";
            options.SaveTokens = true;
            options.Scope.Add("email");
            options.Scope.Add("profile");
        });
}
else
{
    // Không cấu hình Google thì bỏ qua, app vẫn chạy được.
    // (có thể log ra Warning nếu bạn muốn)
}
builder.Services.AddHttpClient<ContactApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Session middleware phải đặt SAU UseRouting và TRƯỚC MapControllerRoute
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); 

app.Run();
