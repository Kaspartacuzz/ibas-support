using IbasSupport.Web.Components;
using IbasSupport.Web.Services;
using IbasSupport.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ISupportRepository>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("Cosmos");
    var conn = cfg["ConnectionString"] ?? throw new InvalidOperationException("Missing Cosmos:ConnectionString");
    var db   = cfg["Database"]        ?? "IBasSupportDB";
    var ctr  = cfg["Container"]       ?? "ibassupport";
    return new CosmosSupportRepository(conn, db, ctr);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
