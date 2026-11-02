using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

Console.WriteLine("FORMA simulation: initializing local host");
var builder=WebApplication.CreateBuilder(args);
Console.WriteLine("FORMA simulation: host configuration loaded");
// Deliberately local development tooling. No public listener, proxy, identity or production backend.
builder.WebHost.ConfigureKestrel(options=>options.Listen(IPAddress.Loopback,
    int.TryParse(Environment.GetEnvironmentVariable("FORMA_SIM_PORT"),out int port)?port:5078));
builder.Logging.ClearProviders();builder.Logging.AddSimpleConsole(o=>o.SingleLine=true);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.ConfigureHttpJsonOptions(o=>SimulationJson.Configure(o.SerializerOptions));
var app=builder.Build();
Console.WriteLine("FORMA simulation: services ready");
var sessions=new ConcurrentDictionary<string,Simulation>();
string dataRoot=Environment.GetEnvironmentVariable("FORMA_SIM_DATA") ?? Path.Combine(Path.GetTempPath(),"forma-web-simulation");
var catalog=Simulation.LoadCatalog(Path.Combine(AppContext.BaseDirectory,"Coaches.json"));
app.Use(async(context,next)=>
{
    if(context.Request.Host.Host!="127.0.0.1" && context.Request.Host.Host!="localhost")
    {context.Response.StatusCode=400;return;}
    context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'none'";
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    context.Response.Headers["Cache-Control"]="no-store";
    if(context.Request.Method=="POST")
    {
        string origin=context.Request.Headers.Origin.ToString();
        if(context.Request.Headers["X-Forma-Simulation"]!="1" ||
            (origin.Length>0 && origin!=$"{context.Request.Scheme}://{context.Request.Host}"))
        {context.Response.StatusCode=403;return;}
    }
    await next(context);
});
app.UseDefaultFiles();app.UseStaticFiles();
Simulation GetSession(HttpContext context)
{
    string id=context.Request.Cookies["forma-sim"];
    if(!Guid.TryParseExact(id,"N",out _))
    {
        id=Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append("forma-sim",id,new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Strict,IsEssential=true,MaxAge=TimeSpan.FromDays(7)});
    }
    if(sessions.Count>=128 && !sessions.ContainsKey(id))throw new InvalidOperationException("Restart the local test host to clear unused sessions.");
    return sessions.GetOrAdd(id,key=>new Simulation(Path.Combine(dataRoot,key),catalog));
}
app.MapGet("/api/state",(HttpContext context)=>
{
    var simulation=GetSession(context);lock(simulation)return Results.Json(simulation.Snapshot());
});
app.MapPost("/api/action",(HttpContext context,SimulationAction action)=>
{
    var simulation=GetSession(context);
    lock(simulation)
    {
        try{return Results.Json(new{accepted=simulation.Act(action),state=simulation.Snapshot()});}
        catch(Exception e) when(e is ArgumentException || e is InvalidOperationException || e is JsonException || e is IOException || e is UnauthorizedAccessException)
        {return Results.Json(new{error="Action could not be applied. Check the current state or local test storage."},statusCode:400);}
    }
});
app.MapGet("/api/export",(HttpContext context)=>
{
    var simulation=GetSession(context);lock(simulation)
        return Results.File(simulation.Export(),"application/json","forma-simulation-export.json");
});
app.MapGet("/health",()=>Results.Json(new{status="ok",engine="shared-csharp",hardwareValidated=false}));
Console.WriteLine("FORMA simulation: starting loopback listener");
app.Run();
