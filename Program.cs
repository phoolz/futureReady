using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor so UserProvider can read the current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserProvider, HttpContextUserProvider>();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// Add EF Core DbContext (SQLite) for multi-tenant data (Schools)
builder.Services.AddDbContext<FutureReady.Data.ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
