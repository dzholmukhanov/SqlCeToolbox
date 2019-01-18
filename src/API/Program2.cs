﻿using System;
using System.Text;
using ErikEJ.SqlCeScripting;

namespace ExportSqlCE
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 6)
            {
                PrintUsageGuide();
                return 2;
            }
            else
            {
                try
                {
                    string connectionString = args[0];
                    string outputFileLocation = args[1];

                    bool includeData = true;
                    bool includeSchema = true;
                    bool saveImageFiles = false;
                    bool keepSchemaName = false;
                    bool preserveDateAndDateTime2 = false;
                    bool sqlite = false;
                    bool toExcludeTables = true;
                    bool toIncludeTables = false;
                    System.Collections.Generic.List<string> exclusions = new System.Collections.Generic.List<string>();
                    System.Collections.Generic.List<string> inclusions = new System.Collections.Generic.List<string>();
                    System.Collections.Generic.List<string> whereClauses = new System.Collections.Generic.List<string>();

                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i].StartsWith("dataonly"))
                        {
                            includeData = true;
                            includeSchema = false;
                        }
                        if (args[i].StartsWith("schemaonly"))
                        {
                            includeData = false;
                            includeSchema = true;
                        }
                        if (args[i].StartsWith("saveimages"))
                            saveImageFiles = true;
                        if (args[i].StartsWith("keepschema"))
                            keepSchemaName = true;
                        if (args[i].StartsWith("preservedateanddatetime2"))
                            preserveDateAndDateTime2 = true;
                        if (args[i].StartsWith("exclude:"))
                        {
                            ParseExclusions(exclusions, args[i], whereClauses);
                            toExcludeTables = true;
                            toIncludeTables = false;
                        }
                        if (args[i].StartsWith("include:"))
                        {
                            ParseInclusions(inclusions, args[i], whereClauses);
                            toIncludeTables = true;
                            toExcludeTables = false;
                        }
                        if (args[i].StartsWith("sqlite"))
                        {
                            sqlite = true;
                            includeSchema = true;
                        }
                    }

                    using (IRepository repository = new ServerDBRepository(connectionString, keepSchemaName))
                    {
                        Helper.FinalFiles = outputFileLocation;
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        var generator = new Generator(repository, outputFileLocation, false, preserveDateAndDateTime2, sqlite);

                        if (toExcludeTables)
                        {
                            generator.ExcludeTables(exclusions);
                        }
                        else if (toIncludeTables)
                        {
                            generator.IncludeTables(inclusions, whereClauses);
                        }

                        if (sqlite)
                        {
                            generator.GenerateSqlitePrefix();
                            if (includeSchema)
                            {
                                generator.GenerateTable(false);
                            }
                            if (includeData)
                            {
                                generator.GenerateTableContent(false);
                            }
                            if (includeSchema)
                            {
                                generator.GenerateIndex();
                            }
                            generator.GenerateSqliteSuffix();
                        }
                        else
                        {
                            // The execution below has to be in this sequence
                            if (includeSchema)
                            {
                                generator.GenerateTable(includeData);
                            }
                            if (includeData)
                            {
                                generator.GenerateTableContent(saveImageFiles);
                            }
                            if (includeSchema)
                            {
                                generator.GeneratePrimaryKeys();
                                generator.GenerateIndex();
                                generator.GenerateForeignKeys();
                            }
                        }
                        Helper.WriteIntoFile(generator.GeneratedScript, outputFileLocation, 1, sqlite);
                        return 0;
                    }
                }
                catch (System.Data.SqlClient.SqlException e)
                {
                    ShowErrors(e);
                    return 1;
                }
                catch (Exception ex)
                {
                    return 1;
                }
            }
        }

        private static void ParseExclusions(System.Collections.Generic.List<string> exclusions, string excludeParam, System.Collections.Generic.List<string> whereClauses)
        {
            ParseTableNames(exclusions, "exclude", excludeParam, whereClauses);
        }

        private static void ParseInclusions(System.Collections.Generic.List<string> inclusions, string includeParam, System.Collections.Generic.List<string> whereClauses)
        {
            ParseTableNames(inclusions, "include", includeParam, whereClauses);
        }

        private static void ParseTableNames(System.Collections.Generic.List<string> tableNames, string argumentName, string argumentParam, System.Collections.Generic.List<string> whereClauses)
        {
            argumentParam = argumentParam.Replace($"{argumentName}:", string.Empty);
            argumentParam = argumentParam.Replace($"\"", string.Empty);
            if (!string.IsNullOrEmpty(argumentParam))
            {
                string[] tables = argumentParam.Split(',');
                foreach (var item in tables)
                {
                    var tableParams = item.Split(':');
                    tableNames.Add(tableParams[0]);
                    whereClauses.Add(tableParams.Length > 1 ? tableParams[1] : null);
                }
            }
        }

        private static void ShowErrors(System.Data.SqlClient.SqlException e)
        {
            System.Data.SqlClient.SqlErrorCollection errorCollection = e.Errors;

            StringBuilder bld = new StringBuilder();
            Exception inner = e.InnerException;

            // Enumerate the errors to a message box.
            foreach (System.Data.SqlClient.SqlError err in errorCollection)
            {
                bld.Append("\n Message   : " + err.Message);
                bld.Append("\n Source    : " + err.Source);
                bld.Append("\n Number    : " + err.Number);

                bld.Remove(0, bld.Length);
            }
        }

        private static void PrintUsageGuide()
        {
            
        }
    }
}
