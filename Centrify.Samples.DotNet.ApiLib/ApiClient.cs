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
using System.Net;
using System.Collections;

namespace Centrify.Samples.DotNet.ApiLib
{
    public class ApiClient
    {
        private RestClient m_restClient = null;

        public ApiClient(RestClient authenticatedClient)
        {
            m_restClient = authenticatedClient;
        }

        public ApiClient(string endpointBase, string bearerToken)
        {
            m_restClient = new RestClient(endpointBase);
            m_restClient.BearerToken = bearerToken;
        }

        public string BearerToken
        {
            get
            {
                if (m_restClient.BearerToken != null)
                {
                    return m_restClient.BearerToken;
                }
                else
                {
                    if (m_restClient.Cookies != null)
                    {
                        CookieCollection endpointCookies = m_restClient.Cookies.GetCookies(new Uri(m_restClient.Endpoint));
                        if (endpointCookies != null)
                        {
                            Cookie bearerCookie = endpointCookies[".ASPXAUTH"];
                            if (bearerCookie != null)
                            {
                                return bearerCookie.Value;
                            }
                        }
                    }
                }
                return null;
            }

            set
            {
                m_restClient.BearerToken = value;
            }
        }

        // Illustrates locking a CUS user via /cdirectoryservice/setuserstate
        public void LockUser(string userUuid)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["ID"] = userUuid;            
            args["state"] = "Locked";

            var result = m_restClient.CallApi("/cdirectoryservice/setuserstate", args);
            if (result["success"] != true)
            {
                Console.WriteLine("LockUser {0} failed: {1}", userUuid, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }
        }

        // Illustrates locking a CUS user via /cdirectoryservice/setuserstate
        public void DisableUser(string userUuid)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["ID"] = userUuid;
            args["state"] = "Disabled";

            var result = m_restClient.CallApi("/cdirectoryservice/setuserstate", args);
            if (result["success"] != true)
            {
                Console.WriteLine("LockUser {0} failed: {1}", userUuid, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }
        }

        // Illustrates unlocking a CUS user via /cdirectoryservice/setuserstate
        public void UnlockUser(string userUuid)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["ID"] = userUuid;
            args["state"] = "None";

            var result = m_restClient.CallApi("/cdirectoryservice/setuserstate", args);
            if (result["success"] != true)
            {
                Console.WriteLine("UnlockUser {0} failed: {1}", userUuid, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }
        }

        public void UpdateRole(string roleUuid, ArrayList users = null, string groups = null, string roles = null)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> argsAdd = new Dictionary<string, dynamic>();
            args["Name"] = roleUuid;
            argsAdd["Add"] = users;
            args["Users"] = argsAdd;

            var result = m_restClient.CallApi("/Roles/UpdateRole", args);
            if (result["success"] != true)
            {
                Console.WriteLine("Add Users And Groups To Role {0} failed: {1}", roleUuid, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }
        }
        // Illustrates usage of /redrock/query to run queries
        public dynamic Query(string sql)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["Script"] = sql;

            Dictionary<string, dynamic> queryArgs = new Dictionary<string, dynamic>();
            args["Args"] = queryArgs;

            /*queryArgs["PageNumber"] = 1;
            queryArgs["PageSize"] = 10000;
            queryArgs["Limit"] = 10000;
            queryArgs["Caching"] = -1;*/

            var result = m_restClient.CallApi("/redrock/query", args);
            if (result["success"] != true)
            {
                Console.WriteLine("Running query failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"];
        }

        // Illustrates usage of /UserMgmt/GetUserRolesAndAdministrativeRights to get user roles
        public dynamic GetUserRolesAndAdministrativeRights(string ID)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["id"] = ID;

            var result = m_restClient.CallApi("/UserMgmt/GetUsersRolesAndAdministrativeRights", args);
            if (result["success"] != true)
            {
                Console.WriteLine("GetUserRolesAndAdministrativeRights failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"];
        }

        // Illustrates usage of /saasManage/updateapplicationde to stash a new username/password for a UP application
        public void UpdateApplicationDE(string appKey, string username, string password)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["UserNameStrategy"] = "Fixed";
            args["Password"] = password;
            args["UserNameArg"] = username;

            var result = m_restClient.CallApi("/saasmanage/updateapplicationde?_RowKey=" + appKey, args);
            if (result["success"] != true)
            {
                Console.WriteLine("Updating app credentials failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }
        }

        // Illustrates usage of /uprest/GetUPData to get a list of all applications
        //  assigned to the current user
        public dynamic GetUPData()
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["force"] = false;

            var result = m_restClient.CallApi("/uprest/getupdata", args);
            if (result["success"] != true)
            {
                Console.WriteLine("Getting apps list failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"]["Apps"];
        }

        // Illustrates usage of /saasmanage/getroleapps to get apps assigned to a role
        public dynamic GetRoleApps(string role)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            var result = m_restClient.CallApi("/saasmanage/getroleapps?role=" + role, args);

            if (result["success"] != true)
            {
                Console.WriteLine("Getting apps list for role {0} failed: {1}", role, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"]["Results"];
        }

        // Illustrates usage of /cdirectoryservice/createuser to create a new CUS user, presumes
        //  username and mail are the same.  Return value is user's UUID
        public Dictionary<string, dynamic> CreateUser(CDUser user, bool inEverybodyRole, bool forcePassChange, bool sendSMS, bool sendEmail, bool passNeverExpire)
        {
            Dictionary<string, dynamic> createUserArgs = new Dictionary<string, dynamic>();
            createUserArgs["Name"] = user.Name;
            createUserArgs["DisplayName"] = user.DisplayName;
            createUserArgs["Mail"] = user.Mail;
            createUserArgs["Description"] = user.Description;
            createUserArgs["OfficeNumber"] = user.OfficeNumber;
            createUserArgs["MobileNumber"] = user.MobileNumber;
            createUserArgs["HomeNumber"] = user.HomeNumber;
            createUserArgs["Password"] = user.Password;
            createUserArgs["ForcePasswordChangeNext"] = forcePassChange;
            createUserArgs["SendEmailInvite"] = sendSMS;
            createUserArgs["SendSmsInvite"] = sendEmail;
            createUserArgs["InEverybodyRole"] = inEverybodyRole;
            createUserArgs["PasswordNeverExpire"] = passNeverExpire;

            var result = m_restClient.CallApi("/cdirectoryservice/createuser", createUserArgs);

            /*if (result["success"] != true)
            {
                Console.WriteLine("Creating user {0} failed: {1}", user.Name, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            //Console.WriteLine("Creating user {0} succeeded.", user.Name);*/
            return result;
        }

        // Illustrates usage of /cdirectoryservice/createuser to create a new CUS user, presumes
        //  username and mail are the same.  Return value is user's UUID
        public Dictionary<string, dynamic> ChangeUser(CDUser user, bool inEverybodyRole)
        {
            Dictionary<string, dynamic> changeUserArgs = new Dictionary<string, dynamic>();
            changeUserArgs["ID"] = user.ID;
            changeUserArgs["Name"] = user.Name;
            changeUserArgs["DisplayName"] = user.DisplayName;
            changeUserArgs["Mail"] = user.Mail;
            changeUserArgs["Description"] = user.Description;
            changeUserArgs["OfficeNumber"] = user.OfficeNumber;
            changeUserArgs["MobileNumber"] = user.MobileNumber;
            changeUserArgs["HomeNumber"] = user.HomeNumber;
            changeUserArgs["InEverybodyRole"] = inEverybodyRole;

            var result = m_restClient.CallApi("/cdirectoryservice/changeuser", changeUserArgs);

            /*if (result["success"] != true)
            {
                Console.WriteLine("Changing user {0} failed: {1}", user.Name, result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            Console.WriteLine("Changing user {0} succeeded.", user.Name);*/
            return result;
        }

        // Illustrates logout
        public void Logout()
        {
            m_restClient.CallApi("/security/logout", new Dictionary<string, dynamic>());
        }
    }

    public class CDUser
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Mail { get; set; }
        public string Description { get; set; }
        public string OfficeNumber { get; set; }
        public string MobileNumber { get; set; }
        public string HomeNumber { get; set; }
        public string Password { get; set; }
    }
}
