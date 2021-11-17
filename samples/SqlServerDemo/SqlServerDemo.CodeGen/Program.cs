﻿using NTangle.Console;
using System.Threading.Tasks;

namespace SqlServerDemo.CodeGen
{
    internal class Program
    {
        // Main invokes the CodeGenConsole passing in a default connection string to the database where CDC is to be enabled (this can be overridden using a command line argument or environment variable).
        public static async Task Main(string[] args) => await CodeGenConsole.Create("Data Source=.;Initial Catalog=NTangleDemo;Integrated Security=True").UseSqlServer().RunAsync(args).ConfigureAwait(false);
    }
}