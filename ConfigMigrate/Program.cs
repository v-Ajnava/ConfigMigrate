// Copyright (c) Kamesh Tanneru. All rights reserved.

namespace ConfigMigrate
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using ConfigMigrate.Models;
    using Microsoft.DocAsCode.Glob;
    using Newtonsoft.Json;
    using Z.Core.Extensions;
    using Z.Data.Extensions;

    class Program
    {
        static void Main(string[] args)
        {
            var settings = Properties.Settings.Default;

            var files = FileGlob.GetFiles(settings.RepositoryPath, new[] { "**/ServiceBusConfigData.sql" }, new string[] { }).ToList();

            foreach (var file in files)
            {
                try
                {
                    CleanupTables(settings.DbConnectionString);
                    GenerateConfigJsonSql(file);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"ERROR: While processing ${file} ${exception.Message}");
                    Console.WriteLine(exception.ToString());
                }
            }

            Console.WriteLine(files.Count);
        }

        private static void GenerateConfigJsonSql(string path, string output = null)
        {
            var info = new FileInfo(path);
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.Combine(info.Directory.FullName, "ClusterConfigs.json");
            }

            Console.WriteLine("Info: Working on directory: " + info.Directory.Name);

            List<ConfigEntry> entries = new List<ConfigEntry>();

            if (File.Exists(output))
            {
                Console.WriteLine($"Information: {output} exists, reading.");
                entries = JsonConvert.DeserializeObject<List<ConfigEntry>>(File.ReadAllText(output));
            }

            using (var connection = new SqlConnection(Properties.Settings.Default.DbConnectionString))
            {
                connection.Open();
                connection.ExecuteSqlScriptFiles(path);
            }

            var list = GetResults(Properties.Settings.Default.DbConnectionString);
            entries.AddRange(list);
            var dict = list.OrderBy(x => x.ConfigEntryKey).ToDictionary(x => x.ConfigEntryKey, x => x);
            File.WriteAllText(output, JsonConvert.SerializeObject(dict.Values));
        }

        private static void CleanupTables(string connectionString)
        {
            var commands = new string[]
            {
                "delete FROM [dbo].[ServiceConfig]",
                "delete FROM [dbo].[Quotas]",
            };

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach(var command in commands)
                {
                    using(var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = command;
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("Executed command: " + command);
                }
            }
        }

        private static List<ConfigEntry> GetResults(string connectionString)
        {
            var entries = new List<ConfigEntry>();
            var builder = new SqlConnectionStringBuilder(connectionString);

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [dbo].[ServiceConfig]";
                    var table = cmd.ExecuteDataTable();
                    foreach (object row in table.Rows)
                    {
                        var config = row as DataRow;

                        string scope = config["Component"].ToString();
                        string name = config["ConfigName"].ToString();
                        string value = config["ConfigValue"].ToString();
                        if (scope.EqualsIgnoreCase("admin"))
                        {
                            scope = "$system";
                            name = "admin." + name;
                        }

                        var entry = new ConfigEntry { ConfigEntryKey = new ConfigEntryKey { ScopeName = scope, ConfigName = name }, ConfigValue = value };
                        entries.Add(entry);
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [dbo].[Quotas]";
                    var table = cmd.ExecuteDataTable();
                    foreach (object row in table.Rows)
                    {
                        var config = row as DataRow;

                        string scope = config["ScopeName"].ToString();
                        string name = config["QuotaName"].ToString();
                        string type = config["ScopeType"].ToString();
                        string value = config["QuotaValue"].ToString();
                        if (scope.EqualsIgnoreCase("admin"))
                        {
                            scope = "$system";
                            name = "admin." + name;
                        }

                        if (!type.Equals("0"))
                        {
                            Console.WriteLine($"WARNING: ScopeType is {type} for Scope: {scope} ConfigName: {name}");
                        }

                        var entry = new ConfigEntry { ConfigEntryKey = new ConfigEntryKey { ScopeName = scope, ConfigName = name }, ConfigValue = value };
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }
    }
}
