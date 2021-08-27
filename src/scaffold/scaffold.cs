// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The `dotnet-ef` and `ef` projects go through many hoops (`dotnet exec`) to run the scaffolder (`dotnet ef dbcontext scaffold`)
// This makes it almost impossible to set a breakpoint and debug the code. This tools has the minimum code required to run the
// scaffolder and is easily debuggable with Visual Studio or Rider.
// This tool was created so that I could investigate https://github.com/dotnet/efcore/issues/25729

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;

try
{
    if (args.Length is not (1 or 2))
    {
        Console.Error.WriteLine($"Usage: {Path.GetFileName(Environment.ProcessPath)} <connectionString> [provider]");
        return 1;
    }

    var connectionString = args[0];
    var provider = args.Length > 1 ? args[1] : "Microsoft.EntityFrameworkCore.SqlServer";

    var operationReportHandler = new OperationReportHandler(
        errorHandler:       s => Console.WriteLine($@"🟥 {s}"),
        warningHandler:     s => Console.WriteLine($@"🟧 {s}"),
        informationHandler: s => Console.WriteLine($@"🟦️ {s}"),
        verboseHandler:     s => Console.WriteLine($@"⬜️ {s}")
    );
    Assembly assembly = typeof(int).Assembly; // System.Private.CoreLib should never have an IDesignTimeServices implementation
    var operations = new DatabaseOperations(new OperationReporter(operationReportHandler),
        assembly: assembly,
        startupAssembly: assembly,
        projectDir: ".",
        rootNamespace: null,
        language: null,
        nullable: true,
        args: null
    );
    var savedModelFiles = operations.ScaffoldContext(
        provider: provider,
        connectionString: connectionString,
        outputDir: "generated",
        outputContextDir: null,
        dbContextClassName: null,
        schemas: Enumerable.Empty<string>(),
        tables: Enumerable.Empty<string>(),
        modelNamespace: null,
        contextNamespace: null,
        useDataAnnotations: false,
        overwriteFiles: true,
        useDatabaseNames: false,
        suppressOnConfiguring: true,
        noPluralize: false
    );
    Console.WriteLine(@"ℹ️ The following files were saved:");
    Console.WriteLine(@"📄 " + savedModelFiles.ContextFile);
    foreach (var additionalFile in savedModelFiles.AdditionalFiles)
    {
        Console.WriteLine(@"📄 " + additionalFile);
    }
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(@"💥 " + exception);
    return 2;
}
