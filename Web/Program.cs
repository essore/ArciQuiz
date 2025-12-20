using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Web.Components;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ArciQuiz");

var dbPath = Path.Combine(AppContext.BaseDirectory, "arciquiz.db");

//builder.Services.AddDbContext<ArciQuizDbContext>(options =>
//    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddDbContextFactory<ArciQuizDbContext>(opt =>
{
    opt.UseSqlite($"Data Source={dbPath}");
});

// Add services to the container.
builder.Services.AddSingleton<IGameStateService, GameStateService>();
builder.Services.AddSingleton<ILanAddressService, LanAddressService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();




// Creazione automatica del database SQLite se non esiste
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArciQuizDbContext>();
    db.Database.EnsureCreated();
    ArciQuizDbInitializer.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{

    // === Apertura automatica browser ===
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            // Usa l'URL effettivamente in ascolto
            // (è lo stesso che usa Visual Studio/launchSettings)
            var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Se non riesce ad aprire il browser, non blocchiamo l'app
        }
    });
    // ================================

}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
