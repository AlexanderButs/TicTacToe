using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using NLog.Web;

namespace TicTacToe
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static async Task Main(string[] args)
        {
            NLog.Web.NLogBuilder.ConfigureNLog("NLog.config");
            try
            {
                await WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>()
                    .UseNLog()
                    .Build()
                    .RunAsync();            
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}