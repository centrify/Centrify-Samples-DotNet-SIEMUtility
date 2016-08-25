# Centrify.Samples.DotNet.SIEMUtility

Notes: This package contains the source code for a sample SIEM Utility for the Centrify Identity Service Platform API's written in C#.  The solution
Centrify.Samples.DotNet.SIEMUtility.sln (VS 2015) contains two projects:
  1. Centrify.Samples.DotNet.ApiLib - Includes a general REST client for communicating with the CIS Platform, as well as
  a class, ApiClient, which uses the REST client to make calls to specific platform API's.
  2. Centrify.Samples.DotNet.SIEMUtility - A sample console application utility which utilizes the ApiLib project to authenticate a user, 
  query the platform API's using queries found in the Queries.json file, and export the results to CSV for a SIEM application to ingest.
  The utility will also back up old output files when it notices that the day has changed since its last run.
  
  This utility can also be used to simply export reports, export analytic information, and aggregate any data within the Centrify Identity Platform data tables. 
  Please see the reports menu in the Centrify Cloud Manager for more information on what tables exist and what data exists in the tables.

# Installation and use Instructions:
 
1. First compile the solution in Release
2. Copy the contents of the Relase folder to a location of your choice
3. Open the Queries.json file and customize desired queries. Default queries are all event log entries for the day and all proxy entries
4. Open the App.config file and customize your Centrify tenant url, admin username, and admin password. Make sure the admin account is set to not use MFA or the utility will fail.
5. Run utility from command line or by double click
6. Results will be located in the Output folder in the root of the utility directory.
7. Use a scheduling tool, such as Windows Task Scheduler, to run the utility on a scheduled basis. The utility can be ran as often as desired as long as it has a chance to finish running before it is ran again.
8. Point SIEM software to Output folder in root and have it ingest the output csv files.
   
