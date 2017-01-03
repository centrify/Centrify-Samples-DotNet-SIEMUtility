/**
 * Copyright 2016 Centrify Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *  http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 **/

using System;
using System.Collections.Generic;
using Centrify.Samples.DotNet.ApiLib;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;

namespace Centrify.Samples.DotNet.QueryUtility
{
    class Query_Utility
    {
        static string Version = "Version 1.3_01_03_17";

        static void Main(string[] args)
        {
            DateTime lastRunTime = DateTime.Now;

            if (File.Exists("LastRun.txt"))
            {
                lastRunTime = Convert.ToDateTime(File.ReadAllText("LastRun.txt"));
            }

            //Output all console logs to log file 
            FileStream filestream = new FileStream("log.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            Console.WriteLine("Starting SIEM Utility... \n");
            Console.WriteLine("Application Version: " + Version);
            Console.WriteLine("Current System Time: " + DateTime.Now + Environment.NewLine);

            Console.WriteLine("Checking last run time and backing up files if needed...");

            if (lastRunTime < DateTime.Today)
            {
                var files = Directory.GetFiles(@".\Output\", "*.csv");
                string prefix = lastRunTime.Month.ToString() + "_" + lastRunTime.Day.ToString() + "_" + lastRunTime.Year.ToString() + "_";
                foreach (var file in files)
                {
                    string newFileName = Path.Combine(Path.GetDirectoryName(file) + "\\Archived\\", (prefix + Path.GetFileName(file)));
                    File.Move(file, newFileName);
                }
            }

            Console.WriteLine("Connecting to Centrify API...");

            //Authenticate to Centrify with no MFA service account
            RestClient authenticatedRestClient = InteractiveLogin.Authenticate(ConfigurationManager.AppSettings["CentrifyEndpointUrl"], ConfigurationManager.AppSettings["AdminUserName"], ConfigurationManager.AppSettings["AdminPassword"]);
            ApiClient apiClient = new ApiClient(authenticatedRestClient);

            Console.WriteLine("Getting list of queries...");

            //Read queries from query file
            StreamReader jsonReader = new StreamReader("Queries.json");
            Dictionary<string, dynamic> queries_Dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonReader.ReadToEnd());

            //Exicute queries in Queries file and export the results to ; delimeted CSV
            foreach (var query in queries_Dict["queries"])
            {
                Console.WriteLine("Exicuting Query {0}... \n", query["caption"]);
                Newtonsoft.Json.Linq.JObject results= apiClient.Query(query["query"].ToString());
                Dictionary<string, dynamic> row_dic = results["Results"][0]["Row"].ToObject<Dictionary<string, dynamic>>();

                bool getRoles = false;

                //Check if the utility should get users roles.
                if(row_dic.ContainsKey(ConfigurationManager.AppSettings["UserIDKey"]) && ConfigurationManager.AppSettings["AppendUserRoles"] == "true")
                {
                    getRoles = true;
                }

                ProcessQueryResults(query["caption"].ToString(), results, getRoles);
            }

            //Log last run time at every successful run.
            try
            {
                System.IO.File.WriteAllText(@".\LastRun.txt", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("There Was An Error When Writing LastRun.txt");
                Console.WriteLine(ex.InnerException);
            }
        }

        static void ProcessQueryResults(string fileName, Newtonsoft.Json.Linq.JObject results, bool GetRoles)
        {
            try
            {
                using (var writer = new StreamWriter(@"Output\" + fileName + ".csv"))
                {
                    //Add Columns to flat file based on the dictionary keys of the first result to ensure column and result order
                    int colCount = 1;
                    string columns = "";
                    Dictionary<string, dynamic> row_dic = results["Results"][0]["Row"].ToObject<Dictionary<string, dynamic>>();
                    List <string> keyList = new List<string>(row_dic.Keys);

                    foreach (var key in keyList)
                    {
                        if (keyList.Count == colCount)
                        {
                            if (GetRoles)
                            {
                                columns = columns + key + ConfigurationManager.AppSettings["CSVDelimiter"] + " "+ "Roles";
                            }
                            else
                            {
                                columns = columns + key;
                            }
                        }
                        else
                        {
                            columns = columns + key + ConfigurationManager.AppSettings["CSVDelimiter"] + " ";

                        }

                        colCount++;
                    }

                    writer.WriteLine(columns);
                    writer.Flush();

                    //Add each row to flat file                   
                    foreach (var result in results["Results"])
                    {
                        Dictionary<string, dynamic> result_Row_Dict = result["Row"].ToObject<Dictionary<string, dynamic>>();

                        int pairCount = 1;
                        string row = "";
                        string rolesList = "";

                        //Get Users Roles if desired
                        if (GetRoles)
                        {
                            //Authenticate to Centrify with no MFA service account
                            RestClient authenticatedRestClient = InteractiveLogin.Authenticate(ConfigurationManager.AppSettings["CentrifyEndpointUrl"], ConfigurationManager.AppSettings["AdminUserName"], ConfigurationManager.AppSettings["AdminPassword"]);
                            ApiClient apiClient = new ApiClient(authenticatedRestClient);

                            Newtonsoft.Json.Linq.JObject roles = apiClient.GetUserRolesAndAdministrativeRights(result_Row_Dict[ConfigurationManager.AppSettings["UserIDKey"]]);
                            Dictionary<string, dynamic> roles_Dict = roles.ToObject<Dictionary<string, dynamic>>();

                            foreach (var role_Entry in roles_Dict["Results"])
                            {
                                Dictionary<string, dynamic> role_Entry_Dict = role_Entry.ToObject<Dictionary<string, dynamic>>();

                                foreach (var role in role_Entry_Dict["Entities"])
                                {
                                    Dictionary<string, dynamic> role_Key_Dict = role.ToObject<Dictionary<string, dynamic>>();
                                    rolesList = rolesList + role_Key_Dict["Key"] + ", ";
                                }
                            }
                        }

                        foreach (var pair in result_Row_Dict)
                        {
                            if (result_Row_Dict.Count == pairCount)
                            {
                                if (GetRoles)
                                {
                                    row = row + pair.Value + ConfigurationManager.AppSettings["CSVDelimiter"] + " " + rolesList.Trim(new char[',']);
                                }
                                else
                                {
                                    row = row + pair.Value;
                                }
                            }
                            else
                            {
                                row = row + pair.Value + ConfigurationManager.AppSettings["CSVDelimiter"] + " ";

                            }

                            pairCount++;
                        }
                        


                        writer.WriteLine(row);
                        writer.Flush();
                    }

                    Console.WriteLine("Query successfully written to CSV...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error processing query to CSV: " + ex.InnerException);
            }
        }
    }
}
