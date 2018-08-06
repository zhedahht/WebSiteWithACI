using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using PythonOnline.Models;

namespace PythonOnline.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            //var message = await GetToken("https://management.core.windows.net/");
            //
            //var rawContent = await message.Content.ReadAsStringAsync();
            //ViewData["Raw"] = rawContent;

            ViewData["Raw"] = "rawContent";

            //JObject jo = JObject.Parse(rawContent);
            //ViewData["Token"] = (string)jo["access_token"];
            ViewData["Token"] = "access_token";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Execute()
        {
            string pythonCode = null;
            try
            {
                pythonCode = HttpContext.Request.Form["Text1"].ToString();

                ViewBag.Result = pythonCode;
            }
            catch (Exception)
            {
                ViewBag.Result = "Wrong Input Provided.";
            }

            var message = await GetToken("https://management.core.windows.net/");
            var rawContent = await message.Content.ReadAsStringAsync();
            ViewData["Raw"] = rawContent;

            JObject jo = JObject.Parse(rawContent);
            string token = (string)jo["access_token"];
            ViewData["Token"] = token;

            try
            {
                var fileContents = System.IO.File.ReadAllText(@".\aciTemplate.json");

                //var token = @"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkZTaW11RnJGTm9DMHNKWEdtdjEzbk5aY2VEYyIsImtpZCI6IkZTaW11RnJGTm9DMHNKWEdtdjEzbk5aY2VEYyJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNTIzMzE4OTAzLCJuYmYiOjE1MjMzMTg5MDMsImV4cCI6MTUyMzMyMjgwMywiYWlvIjoiWTJOZ1lMaSsxR2gyekQ3L3dzNHZOaXZsNmhJM0F3QT0iLCJhcHBpZCI6Ijk5ZjVmNjk4LWVkMzQtNDI0Yy1iNDVjLWIyZmFjNzBmOTg1MiIsImFwcGlkYWNyIjoiMiIsImVfZXhwIjoyNjI4MDAsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0LzcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0Ny8iLCJvaWQiOiIzZGUwNDhhZC0wZDFlLTQ2M2QtOTM4Yy04MGY1N2M1YWZkZWEiLCJzdWIiOiIzZGUwNDhhZC0wZDFlLTQ2M2QtOTM4Yy04MGY1N2M1YWZkZWEiLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1dGkiOiIyNTlqYjVWeTlrZS1KM1NFb1NvSEFBIiwidmVyIjoiMS4wIn0.VvIHCpfIAUj_T2j7KVxescJ2gO2S2Z-gBQlg5eaPSOEBso6DTHjDhBgrLP88Iub7mAzjt9PY5KtnEsTLWCOywq7r6JOiVcwg9YMojYUVmofWz7RlK3g0lJuo0n6iKrqiGss70IC0-TR8XHyG5Sdw_jXy00FSUrcrCdexthB-WsopafZKLsMpvxL8zEbgR1YAINdQYbjyLK5BMenFsONN8xisSTizxyPZesiDnOPhOrb4hOtgh0Xe8v73aczCc5xZWwRlX1kKHIea8igKSO3i1U8udtTPMTNu6yN_W00VkNty7Q7zlYv8dy8rMM7qf1A2q7AQbuEkRnKNBXcA6BrJXQ";
                var putBody = fileContents.Replace("XXX_XXX", pythonCode).Trim(new char['\n']);
                var pubAciResult = await PutACI(token, putBody);

                if (pubAciResult.IsSuccessStatusCode)
                {
                    var gotReulst = false;
                    var retry = 0;
                    while (!gotReulst && retry < 90)
                    {
                        var aciResult = await CallACI(token);
                        var aciResultString = await aciResult.Content.ReadAsStringAsync();
                        ViewBag.Result = aciResultString;

                        gotReulst = !aciResultString.Contains("not available yet.");
                        retry++;
                        Thread.Sleep(1000);
                    }

                }
                else
                {
                    ViewBag.Result = "PUT failed";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Result = "Exception: " + ex;
            }

            return View("Index");
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = /*Activity.Current?.Id*/ new Guid().ToString("N") ?? HttpContext.TraceIdentifier });
        }

        private static async Task<HttpResponseMessage> GetToken(string resource, string apiversion = "2017-09-01")
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Secret", Environment.GetEnvironmentVariable("MSI_SECRET"));
            return await client.GetAsync(String.Format("{0}/?resource={1}&api-version={2}", Environment.GetEnvironmentVariable("MSI_ENDPOINT"), resource, apiversion));
        }

        private static async Task<HttpResponseMessage> CallACI(string token, string apiversion = "2017-09-01")
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string geturl = @"https://management.azure.com/subscriptions/ae43b1e3-c35d-4c8c-bc0d-f148b4c52b78/resourceGroups/demo/providers/Microsoft.ContainerInstance/containerGroups/harryh2/containers/trypython/logs?api-version=2018-04-01";
            return await client.GetAsync(geturl);
        }

        private static async Task<HttpResponseMessage> PutACI(string token, string body)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string putUrl = @"https://management.azure.com/subscriptions/ae43b1e3-c35d-4c8c-bc0d-f148b4c52b78/resourceGroups/demo/providers/Microsoft.ContainerInstance/containerGroups/harryh2?api-version=2018-04-01";
            return await client.PutAsync(putUrl, new StringContent(body, Encoding.UTF8, "application/json"));
        }
    }
}
