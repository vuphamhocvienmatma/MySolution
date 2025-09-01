var builder = WebApplication.CreateBuilder(args);
var reverseProxyConfig = builder.Configuration.GetSection("ReverseProxy");
builder.Services.AddReverseProxy()
    .LoadFromConfig(reverseProxyConfig);
var app = builder.Build();
app.MapReverseProxy();
app.Run();