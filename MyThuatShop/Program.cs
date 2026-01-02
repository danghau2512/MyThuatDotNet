using MyThuatShop.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// HttpClient
builder.Services.AddHttpClient();

// DI services
builder.Services.AddScoped<HomeApiService>();
builder.Services.AddScoped<ProductAPIService>();
builder.Services.AddHttpClient<AccountApiService>();

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
    options.IdleTimeout = TimeSpan.FromSeconds(10); // giống JSP: đủ lâu để test
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
