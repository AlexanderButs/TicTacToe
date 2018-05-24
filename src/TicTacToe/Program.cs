using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TicTacToe
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static Task Main(string[] args) => 
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build()
                .RunAsync();
    }
}
