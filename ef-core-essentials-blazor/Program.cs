using ef_core_essentials_blazor.Components;
using ef_core_essentials_blazor.Data;
using ef_core_essentials_blazor.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpContextAccessor for user context
builder.Services.AddHttpContextAccessor();

// Register DbContext - supports multiple database providers
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
    
    switch (provider)
    {
        case "SqlServer":
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.CommandTimeout(30));
            break;
        // Uncomment and install NuGet packages for other providers:
        // case "PostgreSQL":
        //     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        //     break;
        // case "SQLite":
        //     options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        //     break;
    }
    
    // Enable detailed logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
