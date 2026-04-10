using MortgagePro.Application.Services;
using MortgagePro.WebUI.Hubs;
using MortgagePro.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MortgageDbContext>();

// Tożsamość i logowanie
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<MortgageDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddSignalR(options => {
    options.MaximumReceiveMessageSize = 1024 * 1024; // Zwiększenie limitu dla pełnych harmonogramów
});
builder.Services.AddSingleton<ReactiveMortgageEngine>();

var app = builder.Build();

// Seeding Admin User
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var db = scope.ServiceProvider.GetRequiredService<MortgageDbContext>();
    db.Database.EnsureCreated(); // Wymuszenie stworzenia pełnej schemy z Identity

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    if (await userManager.FindByEmailAsync("admin@admin.com") == null)
    {
        var admin = new IdentityUser { UserName = "admin@admin.com", Email = "admin@admin.com", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<MortgageHub>("/mortgageHub");

app.Run();
