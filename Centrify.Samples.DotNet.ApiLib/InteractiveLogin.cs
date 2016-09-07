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

/*This version of Centrify.Samples.DotNet.ApiLib has been modified from its origional version found here 
 * https://github.com/centrify/centrify-samples-dotnet-cs/tree/master/Centrify.Samples.DotNet.ApiLib to better 
 * fit the needs of the SIEM Utitlity that this version is bundled with. */

using System;
using System.Collections.Generic;

namespace Centrify.Samples.DotNet.ApiLib
{
    public static class InteractiveLogin
    {
        public const string OneTimePassCode = "OTP";
        public const string OathPassCode = "OATH";
        public const string PhoneFactor = "PF";
        public const string Sms = "SMS";
        public const string Email = "EMAIL";
        public const string PasswordReset = "RESET";
        public const string SecurityQuestion = "SQ";

        private static string MechToDescription(dynamic mech)
        {
            string mechName = mech["Name"];
         
            try
            {
                return mech["PromptSelectMech"];
            }
            catch { /* Doesn't support this property */ }
                           
            switch(mechName)
            {
                case "UP":
                    return "Password";                    
                case "SMS":
                    return string.Format("SMS to number ending in {0}", mech["PartialDeviceAddress"]);
                case "EMAIL":
                    return string.Format("Email to address ending with {0}", mech["PartialAddress"]);
                case "PF":
                    return string.Format("Phone call to number ending with {0}", mech["PartialPhoneNumber"]);
                case "OATH":
                    return string.Format("OATH compatible client");
                case "SQ":
                    return string.Format("Security Question");
                default:
                    return mechName;
            }
        }

        // http://stackoverflow.com/questions/3404421/password-masking-console-application
        private static string ReadMaskedPassword()
        {
            ConsoleKeyInfo key;
            string password = null;

            do
            {
                // Read a character without echoing it
                key = Console.ReadKey(true);

                if(key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {                    
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if(key.Key == ConsoleKey.Backspace && password != null)
                    {
                        password = password.Substring(0, password.Length - 1);
                        Console.Write("\b \b");
                    }
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        private static string MechToPrompt(dynamic mech)
        {
            string mechName = mech["Name"];
            try
            {
                return mech["PromptMechChosen"];
            }
            catch { /* Doesn't support this property */ }
            switch (mechName)
            {
                case "UP":
                    return "Password: ";
                case "SMS":
                    return string.Format("Enter the code sent via SMS to number ending in {0}: ", mech["PartialDeviceAddress"]);
                case "EMAIL":
                    return string.Format("Please click or open the link sent to the email to address ending with {0}.", mech["PartialAddress"]);
                case "PF":
                    return string.Format("Calling number ending with {0}, please follow the spoken prompt.", mech["PartialPhoneNumber"]);
                case "OATH":
                    return string.Format("Enter your current OATH code: ");
                case "SQ":
                    return string.Format("Enter the response to your secret question: ");
                default:
                    return mechName;
            }
        }

        private static void AdvanceForMech(RestClient client, string tenantId, string sessionId, dynamic mech, string adminPassword)
        {
            Dictionary<string, dynamic> advanceArgs = new Dictionary<string, dynamic>();
            advanceArgs["TenantId"] = tenantId;
            advanceArgs["SessionId"] = sessionId;
            advanceArgs["MechanismId"] = mech["MechanismId"];
            advanceArgs["PersistentLogin"] = false;            

            // Write prompt
            //Console.Write(MechToPrompt(mech));

            // Read or poll for response.  For StartTextOob we simplify and require user to enter the response, rather
            //  than simultaenously prompting and polling, though this can be done as well.
            string answerType = mech["AnswerType"];            
            switch (answerType)
            {
                case "Text":
                case "StartTextOob":
                    {
                        if (answerType == "StartTextOob")
                        {
                            // First we start oob, to get the mech activated
                            advanceArgs["Action"] = "StartOOB";
                            client.CallApi("/security/advanceauthentication", advanceArgs);
                        }

                        // Now prompt for the value to submit and do so
                        advanceArgs["Answer"] = adminPassword;
                        advanceArgs["Action"] = "Answer";
                        var result = client.CallApi("/security/advanceauthentication", advanceArgs);
                        if (!result["success"] ||
                            !(result["Result"]["Summary"] == "StartNextChallenge" ||
                              result["Result"]["Summary"] == "LoginSuccess"))
                        {                            
                            throw new ApplicationException(result["Message"]);
                        }
                    }
                    break;
                case "StartOob":
                    // Pure out of band mech, simply poll until complete or fail                    
                    advanceArgs["Action"] = "StartOOB";
                    client.CallApi("/security/advanceauthentication", advanceArgs);

                    // Poll
                    advanceArgs["Action"] = "Poll";
                    Dictionary<string, dynamic> pollResult = new Dictionary<string, dynamic>();
                    do
                    {
                        Console.Write(".");
                        pollResult = client.CallApi("/security/advanceauthentication", advanceArgs);
                        System.Threading.Thread.Sleep(1000);
                    } while (pollResult["success"] && pollResult["Result"]["Summary"] == "OobPending");

                    // We are done polling, did it work?
                    if (!pollResult["success"] ||
                        !(pollResult["Result"]["Summary"] == "StartNextChallenge" ||
                          pollResult["Result"]["Summary"] == "LoginSuccess"))
                    {
                        throw new ApplicationException(pollResult["Message"]);
                    }
                    break;
            }
        }

        // Performs an MFA login interactively using Console
        public static RestClient Authenticate(string podEndpoint, string adminUserName, string adminPassword)
        {
            RestClient client = new RestClient(podEndpoint);

            // /security/startauthentication api takes username and version:
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["User"] = adminUserName;
            args["Version"] = "1.0";
            Dictionary<string, dynamic> startResult = client.CallApi("/security/startauthentication", args);

            // First thing to check for is whether we should repeat the call against a more specific pod name (tenant specific url):
            if (startResult["success"] && startResult["Result"]["PodFqdn"] != null)
            {
                Console.WriteLine("Auth redirected to {0}", startResult["Result"]["PodFqdn"]);
                client.Endpoint = string.Format("https://{0}", startResult["Result"]["PodFqdn"]);
                startResult = client.CallApi("/security/startauthentication", args);
            }

            // Get the session id to use in handshaking for MFA
            string authSessionId = startResult["Result"]["SessionId"];
            string tenantId = startResult["Result"]["TenantId"];

            // Also get the collection of challenges we need to satisfy
            var challengeCollection = startResult["Result"]["Challenges"];

            // We need to satisfy one of each challenge collection:
            for(int x = 0; x < challengeCollection.Count; x++)
            {
                // Present the option(s) to the user
                for(int mechIdx = 0; mechIdx < challengeCollection[x]["Mechanisms"].Count; mechIdx++)
                {                    
                    Console.WriteLine("Option {0}: {1}", mechIdx, MechToDescription(challengeCollection[x]["Mechanisms"][mechIdx]));
                }
                
                int optionSelect = -1;

                if (challengeCollection[x]["Mechanisms"].Count == 1)
                {
                    optionSelect = 0;
                }
                else
                {
                    while (optionSelect < 0 || optionSelect > challengeCollection[x]["Mechanisms"].Count)
                    {
                        Console.Write("Select option and press enter: ");
                        string optEntered = Console.ReadLine();
                        int.TryParse(optEntered, out optionSelect);
                    }
                }

                AdvanceForMech(client, tenantId, authSessionId, challengeCollection[x]["Mechanisms"][optionSelect], adminPassword);
            }            

            return client;                        
        }
    }
}
