#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Design, 8.0.0"

using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

// Test if migration works
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrowserApp", "browser.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

Console.WriteLine($"Testing database at: {dbPath}");

try
{
    var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
    optionsBuilder.UseSqlite($"Data Source={dbPath}");

    using var context = new DbContext(optionsBuilder.Options);

    Console.WriteLine("Attempting migration...");
    context.Database.Migrate();
    Console.WriteLine("Migration succeeded!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}
