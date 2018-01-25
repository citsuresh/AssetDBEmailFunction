
using System;
using System.Configuration;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AssetDBEmailFunction
{
    public static class AssetAddedEmailFunction
    {
        [FunctionName("AssetAddedEmailFunction")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            bool ismailsentsuccess = false;

            log.Info("C# HTTP trigger function processed a request.");

            string name = req.Query["Name"];
            string assettype = req.Query["AssetTypeValue"];
            string assetsubtypes = req.Query["AssetSubTypeValue"];
            string clientid = req.Query["ClientID"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject<Asset>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.AssetTypeValue) || string.IsNullOrEmpty(data.AssetSubTypeValue))
            {
                return new BadRequestObjectResult("Please pass a valid asset json in request body");
            }

            name = name ?? data?.Name;
            assettype = assettype ?? data?.AssetTypeValue;
            assetsubtypes = assetsubtypes ?? data?.AssetSubTypeValue;
            clientid = clientid ?? data?.ClientID;

            var apiKey = GetEnvironmentVariable("sendgrid_api_key");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new BadRequestObjectResult("unable to get sendgrid_api_key from settings.");
            }
            else
            {
                try
                {
                    var client = new SendGridClient(apiKey);
                    var from = new EmailAddress("suresh.kumar@mt.com", "Asset DB Admin");
                    var subject = "AssetDB: Asset added";
                    var to = new EmailAddress("suresh.kumar@mt.com", "Receiver");
                    var plainTextContent = "New asset Added by \n Client : " + clientid + "\n Asset Type : " + assettype +
                                           "\n Asset Sub Type : " + assetsubtypes;
                    var htmlContent = "<strong>" + "New asset Added by <br/> Client : " + clientid + "<br/> Asset Type : " +
                                      assettype + "<br/> Asset Sub Type : " + assetsubtypes + "</strong>";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                    var response = client.SendEmailAsync(msg);
                    ismailsentsuccess = true;
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult("Email send Failed.\n" + e.Message + "\n" + e.StackTrace);
                }
            }

            return ismailsentsuccess 
                ? (ActionResult)new OkObjectResult($" Email sent successfully.")
                : new BadRequestObjectResult("Email send Failed.");
        }

        public static string GetEnvironmentVariable(string name)
        {
            return /*name + ": " +*/
                   System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
