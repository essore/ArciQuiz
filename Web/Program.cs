using Core.Services;
using Infrasctructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Web.Components;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var adminCredentials = builder.Configuration.GetSection(AdminCredentials.SectionName).Get<AdminCredentials>() ?? new AdminCredentials();
if (string.IsNullOrWhiteSpace(adminCredentials.Username) || string.IsNullOrWhiteSpace(adminCredentials.Password))
    throw new InvalidOperationException("Configurare Admin:Username e Admin:Password in appsettings.Local.json o nelle variabili d'ambiente.");

// Disabilita EventLog di Windows per evitare requisiti di privilegi di amministrazione a runtime
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var databaseFileName = builder.Configuration["Database:FileName"] ?? "arciquiz.db";
var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ArciQuiz");
Directory.CreateDirectory(dataDirectory);

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = Path.Combine(dataDirectory, databaseFileName)
}.ToString();

builder.Services.AddDbContextFactory<ArciQuizDbContext>(opt =>
{
    opt.UseSqlite(connectionString);
});
builder.Services.AddSingleton(adminCredentials);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = PlayerSessionAuthentication.SelectorScheme;
        options.DefaultChallengeScheme = PlayerSessionAuthentication.SelectorScheme;
    })
    .AddPolicyScheme(PlayerSessionAuthentication.SelectorScheme, null, options =>
    {
        options.ForwardDefaultSelector = context => PlayerSessionAuthentication.SelectScheme(
            context.Request.Path,
            context.Request.Cookies.ContainsKey(PlayerSessionAuthentication.CookieName));
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "ArciQuiz.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    })
    .AddCookie(PlayerSessionAuthentication.Scheme, options =>
    {
        options.LoginPath = "/squadra/accesso";
        options.Cookie.Name = PlayerSessionAuthentication.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PlayerSessionAuthentication.AuthorizationPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(PlayerSessionAuthentication.PlayerIdClaimType);
    });
});

// Add services to the container.
builder.Services.AddSingleton<IGameStateService, GameStateService>();
builder.Services.AddSingleton<ILanAddressService, LanAddressService>();
builder.Services.AddSingleton<ILanUrlService, LanUrlService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<GameTimerBackgroundService>();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();




// Applica lo schema persistente prima di usare il database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArciQuizDbContext>();
    db.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        ArciQuizDbInitializer.Seed(db);
    }
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
    //app.Lifetime.ApplicationStarted.Register(() =>
    //{
    //    try
    //    {
    //        // Usa l'URL effettivamente in ascolto
    //        // (è lo stesso che usa Visual Studio/launchSettings)
    //        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";

    //        Process.Start(new ProcessStartInfo
    //        {
    //            FileName = url,
    //            UseShellExecute = true
    //        });
    //    }
    //    catch
    //    {
    //        // Se non riesce ad aprire il browser, non blocchiamo l'app
    //    }
    //});
    // ================================

}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (AdminAccessService.RequiresAuthentication(context.Request.Path)
        && !(context.User.Identity?.IsAuthenticated ?? false))
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/login", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(LoginPage(token, null), "text/html; charset=utf-8");
});
app.MapPost("/login", async (HttpContext context, IAntiforgery antiforgery, AdminCredentials credentials) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    if (!AdminAccessService.IsValid(credentials, form["username"], form["password"]))
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        return Results.Content(LoginPage(token, "Credenziali non valide."), "text/html; charset=utf-8", statusCode: StatusCodes.Status401Unauthorized);
    }

    var claims = new[] { new Claim(ClaimTypes.Name, credentials.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/admin");
});
app.MapGet("/logout", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(LogoutPage(token), "text/html; charset=utf-8");
});
app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
app.MapGet("/gioca", async (HttpContext context, IDbContextFactory<ArciQuizDbContext> dbFactory) =>
{
    var authentication = await context.AuthenticateAsync(PlayerSessionAuthentication.Scheme);
    var playerIdValue = authentication.Principal?.FindFirst(PlayerSessionAuthentication.PlayerIdClaimType)?.Value;
    var sessionToken = authentication.Principal?.FindFirst(PlayerSessionAuthentication.SessionTokenClaimType)?.Value;
    int? playerId = int.TryParse(playerIdValue, out var parsedPlayerId) ? parsedPlayerId : null;

    await using var db = await dbFactory.CreateDbContextAsync();
    var destination = await PlayerEntryService.ResolveAsync(db, playerId, sessionToken);
    if (destination.Destination == PlayerEntryDestination.TeamArea)
        return Results.Redirect("/squadra");

    if (authentication.Succeeded)
        await context.SignOutAsync(PlayerSessionAuthentication.Scheme);

    return destination.Destination switch
    {
        PlayerEntryDestination.RegistrationOrLogin => Results.Content(PlayerEntryPage(), "text/html; charset=utf-8"),
        PlayerEntryDestination.Login => Results.Redirect("/squadra/accesso"),
        _ => Results.Content(NoGamePage(), "text/html; charset=utf-8")
    };
});
app.MapGet("/squadra/registrazione", async (HttpContext context, IAntiforgery antiforgery, IDbContextFactory<ArciQuizDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var entry = await PlayerEntryService.ResolveAsync(db, null, null);
    if (entry.Destination != PlayerEntryDestination.RegistrationOrLogin)
        return Results.Redirect("/gioca");

    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(PlayerRegistrationPage(token, null), "text/html; charset=utf-8");
});
app.MapPost("/squadra/registrazione", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IDbContextFactory<ArciQuizDbContext> dbFactory,
    TimeProvider timeProvider) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    await using var db = await dbFactory.CreateDbContextAsync();
    var registration = await SquadreService.RegistraAsync(db, null, form["nomeSquadra"], form["password"], isAdmin: false);
    if (!registration.IsSuccess)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        return Results.Content(PlayerRegistrationPage(token, registration.Messaggio), "text/html; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
    }

    var login = await PlayerSessionService.AccediAsync(db, null, form["nomeSquadra"], form["password"]);
    if (!login.IsSuccess)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        return Results.Content(PlayerRegistrationPage(token, login.Messaggio), "text/html; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
    }

    await context.SignInAsync(
        PlayerSessionAuthentication.Scheme,
        PlayerSessionAuthentication.CreatePrincipal(login.SquadraId!.Value, login.SessionToken!),
        PlayerSessionAuthentication.CreatePersistentProperties(timeProvider));
    return Results.Redirect("/squadra");
});
app.MapGet("/squadra/accesso", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(PlayerLoginPage(token, null), "text/html; charset=utf-8");
});
app.MapPost("/squadra/accesso", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IDbContextFactory<ArciQuizDbContext> dbFactory,
    TimeProvider timeProvider) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    await using var db = await dbFactory.CreateDbContextAsync();
    var result = await PlayerSessionService.AccediAsync(db, null, form["nomeSquadra"], form["password"]);
    if (!result.IsSuccess)
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        return Results.Content(PlayerLoginPage(token, result.Messaggio), "text/html; charset=utf-8", statusCode: StatusCodes.Status401Unauthorized);
    }

    await context.SignInAsync(
        PlayerSessionAuthentication.Scheme,
        PlayerSessionAuthentication.CreatePrincipal(result.SquadraId!.Value, result.SessionToken!),
        PlayerSessionAuthentication.CreatePersistentProperties(timeProvider));
    return Results.Redirect("/squadra");
});
app.MapGet("/squadra/esci", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(PlayerLogoutPage(token), "text/html; charset=utf-8");
});
app.MapPost("/squadra/esci", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(PlayerSessionAuthentication.Scheme);
    return Results.Redirect("/squadra/accesso");
});
app.MapGet("/api/domande/export", async (IDbContextFactory<ArciQuizDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var domande = await db.Domande
        .AsNoTracking()
        .Where(domanda => !domanda.FlgDeleted)
        .OrderBy(domanda => domanda.Id)
        .ToListAsync();

    var contenuto = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(DomandeCsvService.Export(domande))).ToArray();
    return Results.File(contenuto, "text/csv; charset=utf-8", "domande.csv");
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

static string LoginPage(string? antiforgeryToken, string? error)
{
    var errorMessage = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p role=\"alert\">{System.Net.WebUtility.HtmlEncode(error)}</p>";
    return $"""
        {PlayerPageHead("Login admin")}<body><main><h1>Login admin</h1>
        <p>Accedi alla configurazione e alla regia della partita.</p>{errorMessage}
        <form method="post" action="/login"><input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
        <label>Utente<input name="username" autocomplete="username" required></label>
        <label>Password<input type="password" name="password" autocomplete="current-password" required></label>
        <button type="submit">Accedi come admin</button></form>
        <a class="action secondary" href="/squadra/accesso">Vai al login squadra</a></main></body></html>
        """;
}

static string LogoutPage(string? antiforgeryToken) => $"""
    <!DOCTYPE html><html lang="it"><head><meta charset="utf-8"><title>Esci</title></head>
    <body><main><h1>Esci dall'area amministrativa</h1><form method="post" action="/logout">
    <input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
    <button type="submit">Esci</button></form></main></body></html>
    """;

static string PlayerLoginPage(string? antiforgeryToken, string? error)
{
    var errorMessage = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p role=\"alert\">{System.Net.WebUtility.HtmlEncode(error)}</p>";
    return $"""
        {PlayerPageHead("Login squadra")}<body><main><h1>Login squadra</h1>
        <p>Inserite le credenziali scelte durante l'iscrizione.</p>{errorMessage}
        <form method="post" action="/squadra/accesso"><input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
        <label>Nome squadra<input name="nomeSquadra" autocomplete="username" maxlength="80" required></label>
        <label>Password<input type="password" name="password" autocomplete="current-password" maxlength="100" required></label>
        <button type="submit">Entra nella partita</button></form>
        <a class="action secondary" href="/gioca">Torna all'ingresso squadra</a>
        <a class="action secondary" href="/login">Vai al login admin</a></main></body></html>
        """;
}

static string PlayerRegistrationPage(string? antiforgeryToken, string? error)
{
    var errorMessage = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p role=\"alert\">{System.Net.WebUtility.HtmlEncode(error)}</p>";
    return $"""
        {PlayerPageHead("Iscrizione squadra")}<body><main><h1>Iscrivi la squadra</h1>
        <p>Scegliete nome e password. Dopo la conferma entrerete direttamente nella partita.</p>{errorMessage}
        <form method="post" action="/squadra/registrazione"><input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
        <label>Nome squadra<input name="nomeSquadra" autocomplete="username" maxlength="80" required></label>
        <label>Password<input type="password" name="password" autocomplete="new-password" maxlength="100" required></label>
        <button type="submit">Iscriviti e continua</button></form>
        <a class="action secondary" href="/gioca">Torna indietro</a></main></body></html>
        """;
}

static string PlayerEntryPage() => $"""
    {PlayerPageHead("Partecipa ad ArciQuiz")}<body><main><h1>Partecipa ad ArciQuiz</h1>
    <p>Scegli come entrare nella partita.</p>
    <a class="action" href="/squadra/registrazione">Nuova squadra</a>
    <a class="action secondary" href="/squadra/accesso">Squadra già iscritta</a>
    </main></body></html>
    """;

static string NoGamePage() => $"""
    {PlayerPageHead("ArciQuiz")}<body><main><h1>Nessuna partita disponibile</h1>
    <p>Attendi le indicazioni del presentatore e riprova dal QR.</p>
    <a class="action secondary" href="/gioca">Riprova</a></main></body></html>
    """;

static string PlayerPageHead(string title) => $$"""
    <!DOCTYPE html><html lang="it"><head><meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{System.Net.WebUtility.HtmlEncode(title)}}</title>
    <style>
    * { box-sizing: border-box; } body { margin: 0; background: #f4f6f8; color: #172033; font-family: system-ui, sans-serif; }
    main { width: min(100%, 32rem); min-height: 100vh; margin: 0 auto; padding: 2rem 1.25rem; background: white; }
    h1 { font-size: clamp(2rem, 9vw, 3rem); line-height: 1.05; } p { font-size: 1.1rem; line-height: 1.5; }
    label { display: block; margin: 1.25rem 0; font-weight: 700; } input { width: 100%; min-height: 3.25rem; margin-top: .4rem; padding: .75rem; font: inherit; border: 2px solid #aab3c2; border-radius: .75rem; }
    button, .action { display: block; width: 100%; min-height: 3.5rem; margin-top: 1rem; padding: .9rem; border: 0; border-radius: .8rem; background: #1457d9; color: white; font: inherit; font-weight: 800; text-align: center; text-decoration: none; }
    .secondary { background: #e8edf5; color: #172033; } [role=alert] { padding: 1rem; border-radius: .75rem; background: #ffe4e4; color: #8b1515; }
    </style></head>
    """;

static string PlayerLogoutPage(string? antiforgeryToken) => $"""
    <!DOCTYPE html><html lang="it"><head><meta charset="utf-8"><title>Esci dalla squadra</title></head>
    <body><main><h1>Esci dalla squadra</h1><form method="post" action="/squadra/esci">
    <input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
    <button type="submit">Esci</button></form></main></body></html>
    """;
