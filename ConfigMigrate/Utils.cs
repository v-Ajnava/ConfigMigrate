// Copyright (c) Kamesh Tanneru. All rights reserved.

namespace ConfigMigrate
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;

    using Z.Core.Extensions;

    public static class Utils
    {
        public static void ExecuteSqlScriptFiles(this SqlConnection connection, params string[] sqlScriptFiles)
        {
            foreach (string scriptCommand in GetScriptCommands(sqlScriptFiles))
            {
                using (SqlCommand sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = scriptCommand;
                    sqlCommand.CommandTimeout = 300;
                    bool flag = true;
                    int num = 5;
                    while (true)
                    {
                        if (!flag)
                        {
                            return;
                        }

                        if (num == 0)
                        {
                            break;
                        }

                        flag = false;
                        try
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205)
                            {
                                flag = true;
                                num--;
                            }
                            else if (ex.Errors[0].Number != 2714)
                            {
                                throw;
                            }
                        }
                    }

                    throw new Exception("All retries over");
                }
            }
        }

        private static IEnumerable<string> GetScriptCommands(params string[] sqlScriptFiles)
        {
            int num = 0;
            string text;
            while (true)
            {
                if (num < sqlScriptFiles.Length)
                {
                    text = sqlScriptFiles[num];
                    if (!File.Exists(text))
                    {
                        break;
                    }

                    using (TextReader stringReader = new StreamReader(text))
                    {
                        bool keepGoing = true;
                        string text2 = null;
                        while (keepGoing)
                        {
                            string text3 = stringReader.ReadLine();
                            if (text3 == null)
                            {
                                keepGoing = false;
                            }
                            else if (text3.Trim().EqualsIgnoreCase("go"))
                            {
                                if (!string.IsNullOrEmpty(text2))
                                {
                                    yield return text2;
                                }
                                text2 = null;
                            }
                            else
                            {
                                text2 = text2 + text3 + Environment.NewLine;
                            }
                        }

                        if (text2 != null)
                        {
                            yield return text2;
                        }
                    }

                    num++;
                    continue;
                }

                yield break;
            }
        }

    }
}
