﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/NTangle

using CoreEx.Database.SqlServer;
using Microsoft.Data.SqlClient;
using OnRamp;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;

namespace NTangle.Console
{
    /// <summary>
    /// <b>NTangle</b>-specific code-generation console that inherits from <see cref="OnRamp.Console.CodeGenConsole"/>.
    /// </summary>
    public class CodeGenConsole : OnRamp.Console.CodeGenConsole
    {
        /// <summary>
        /// Gets the default configuration file name (see <see cref="ICodeGeneratorArgs.ConfigFileName"/>).
        /// </summary>
        public const string DefaultConfigFileName = "ntangle.yaml";

        /// <summary>
        /// Gets the default masthead text.
        /// </summary>
        /// <remarks>Defaults to 'NTangle Code-Gen Tool' formatted using <see href="http://www.patorjk.com/software/taag/#p=display&amp;f=Calvin%20S&amp;t=NTangle%20Code-Gen%20Tool%0A"/>.</remarks>
        public const string DefaultMastheadText = @"
╔╗╔╔╦╗┌─┐┌┐┌┌─┐┬  ┌─┐  ╔═╗┌─┐┌┬┐┌─┐  ╔═╗┌─┐┌┐┌  ╔╦╗┌─┐┌─┐┬  
║║║ ║ ├─┤││││ ┬│  ├┤   ║  │ │ ││├┤───║ ╦├┤ │││   ║ │ ││ ││  
╝╚╝ ╩ ┴ ┴┘└┘└─┘┴─┘└─┘  ╚═╝└─┘─┴┘└─┘  ╚═╝└─┘┘└┘   ╩ └─┘└─┘┴─┘
";

        /// <summary>
        /// Creates a new instance of the <see cref="CodeGenConsole"/> class.
        /// </summary>
        /// <param name="connectionString">The default connection string.</param>
        /// <param name="assemblies">The list of additional assemblies to probe for resources; where none are passed then <see cref="Assembly.GetCallingAssembly"/> will be added as default</param>
        /// <remarks>Also adds this <see cref="Assembly"/> before adding the passed <paramref name="assemblies"/> into <see cref="CodeGeneratorArgsBase.Assemblies"/>.</remarks>
        public CodeGenConsole(string connectionString, params Assembly[] assemblies) : base()
        {
            Args.ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Args.AddAssembly(typeof(DbEx.DatabaseExtensions).Assembly).AddAssembly(typeof(DbEx.SqlServer.SqlServerSchemaConfig).Assembly).AddAssembly(typeof(CodeGenConsole).Assembly);
            if (assemblies == null || assemblies.Length == 0)
                Args.AddAssembly(Assembly.GetCallingAssembly());
            else
                Args.AddAssembly(assemblies);

            Args.OutputDirectory = new DirectoryInfo(GetBaseExeDirectory()).Parent;
            Args.CreateConnectionStringEnvironmentVariableName ??= _ => $"{Args.GetAppName()?.Replace(".", "_", StringComparison.InvariantCulture)}_ConnectionString";
            Args.ConfigFileName ??= DefaultConfigFileName;
            MastheadText = DefaultMastheadText;

            Args.SetCreateDatabase((cs) => new SqlServerDatabase(() => new SqlConnection(cs)));
            UseDeploymentOption(DeploymentOption.DbEx);
        }

        /// <summary>
        /// Uses (overrides) the execution script file or embedded resource name.
        /// </summary>
        /// <param name="scriptFileName">The execution script file or embedded resource name.</param>
        /// <returns>The current instance to support fluent-style method-chaining.</returns>
        public CodeGenConsole UseScript(string scriptFileName)
        {
            Args.ScriptFileName = scriptFileName ?? throw new ArgumentNullException(scriptFileName);
            return this;
        }

        /// <summary>
        /// Uses (overrides) the configuration file name (defaults to <see cref="DefaultConfigFileName"/>).
        /// </summary>
        /// <param name="configFileName">The configuration file name.</param>
        /// <returns>The current instance to support fluent-style method-chaining.</returns>
        public CodeGenConsole UseConfig(string configFileName)
        {
            Args.ConfigFileName = configFileName ?? throw new ArgumentNullException(configFileName);
            return this;
        }

        /// <summary>
        /// Uses (overrides) the <see cref="UseScript(string)">script</see> according to the <paramref name="deploymentOption"/>.
        /// </summary>
        /// <param name="deploymentOption">The <see cref="DeploymentOption"/> option.</param>
        /// <returns>The current instance to support fluent-style method-chaining.</returns>
        /// <remarks>Defaults to <see cref="DeploymentOption.DbEx"/>.</remarks>
        public CodeGenConsole UseDeploymentOption(DeploymentOption deploymentOption)
        {
            UseScript($"SqlServer{deploymentOption}.yaml");
            return this;
        }

        /// <inheritdoc/>
        protected override ValidationResult? OnValidation(ValidationContext context)
        {
            if (string.IsNullOrEmpty(Args.GetAppName()))
                Args.SetAppName(Args.OutputDirectory?.Name ?? "APP-NAME-UNKNOWN");

            return base.OnValidation(context);
        }
    }
}