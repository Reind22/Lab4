using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicTacToe.Server;
using TicTacToe.Server.Game;
using TicTacToe.Server.Networking;
using TicTacToe.Server.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<ServerOptions>()
    .Bind(builder.Configuration.GetSection(ServerOptions.SectionName));

builder.Services.AddDbContext<LeaderboardDbContext>((sp, options) =>
{
    var serverOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServerOptions>>().Value;
    options.UseSqlite($"Data Source={serverOptions.DbPath}");
});

builder.Services.AddScoped<LeaderboardRepository>();
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddScoped<ClientHandler>();
builder.Services.AddSingleton<TcpServer>();
builder.Services.AddHostedService<TcpServerHost>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaderboardDbContext>();
    db.Database.EnsureCreated();
}

await host.RunAsync();
