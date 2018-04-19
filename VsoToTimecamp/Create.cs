
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace VsoToTimecamp
{
    public static class Create
    {
        [PublicAPI]
        [FunctionName(nameof(Create))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "create/{token}")]HttpRequest req, string token, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var requestBody = new StreamReader(req.Body).ReadToEnd();
            var data = JsonConvert.DeserializeObject<WorkItem>(requestBody);

            if (!TimecampClient.IsAcceptedType(data.Resource.Fields.SystemWorkItemType)) return new OkResult();

            try
            {
                var response = await TimecampClient.CreateAsync(token, data.Message.Text, data.Id).ConfigureAwait(false);
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return new ExceptionResult(ex, true);
            }
        }
    }

    static class TimecampClient
    {
        public static bool IsAcceptedType(string type) =>
            type.Equals("Bug", StringComparison.InvariantCultureIgnoreCase) ||
            type.Equals("Task", StringComparison.InvariantCultureIgnoreCase);

        public static async Task<dynamic> CreateAsync(string token, string name, string vsoId)
        {
            var uri = "tasks/api_token/" + token;
            var payload = new MultipartFormDataContent
            {
                { new StringContent(name), "name" },
                { new StringContent(vsoId), "external_task_id" }
            };
            var response = await PostAsync(uri, payload).ConfigureAwait(false);
            var newTask = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response);
            if (newTask.Keys.Count == 1 && int.TryParse(newTask.Keys.Single(), out var _))
                return newTask.Values.First();
            throw new Exception(response);
        }


        static async Task<string> PostAsync(string uri, MultipartFormDataContent payload)
        {
            using (var http = new HttpClient { BaseAddress = new Uri("https://www.timecamp.com/third_party/api/") })
            {
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await http.PostAsync(uri, payload).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw new Exception($"Time Camp response: {response.StatusCode}");
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return responseBody;
            }
        }
    }

    public class WorkItem
    {
        public string SubscriptionId { get; set; }
        public int NotificationId { get; set; }
        public string Id { get; set; }
        public string EventType { get; set; }
        public string PublisherId { get; set; }
        public Message Message { get; set; }
        public object DetailedMessage { get; set; }
        public Resource Resource { get; set; }
        public string ResourceVersion { get; set; }
        public Resourcecontainers ResourceContainers { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Message
    {
        public string Text { get; set; }
    }

    public class Resource
    {
        public int Id { get; set; }
        public int Rev { get; set; }
        public Fields Fields { get; set; }
        public Links Links { get; set; }
        public string Url { get; set; }
    }

    public class Fields
    {
        public string SystemAreaPath { get; set; }
        public string SystemTeamProject { get; set; }
        public string SystemIterationPath { get; set; }

        [JsonProperty(PropertyName = "System.WorkItemType")]
        public string SystemWorkItemType { get; set; }
        public string SystemState { get; set; }
        public string SystemReason { get; set; }
        public DateTime SystemCreatedDate { get; set; }
        public string SystemCreatedBy { get; set; }
        public DateTime SystemChangedDate { get; set; }
        public string SystemChangedBy { get; set; }
        public string SystemTitle { get; set; }
        public string MicrosoftVstsCommonSeverity { get; set; }
        public string WefEb329F44Fe5F4A94Acb1Da153Fdf38BaKanbanColumn { get; set; }
    }

    public class Links
    {
        public Self Self { get; set; }
        public Workitemupdates WorkItemUpdates { get; set; }
        public Workitemrevisions WorkItemRevisions { get; set; }
        public Workitemtype WorkItemType { get; set; }
        public Fields1 Fields { get; set; }
    }

    public class Self
    {
        public string Href { get; set; }
    }

    public class Workitemupdates
    {
        public string Href { get; set; }
    }

    public class Workitemrevisions
    {
        public string Href { get; set; }
    }

    public class Workitemtype
    {
        public string Href { get; set; }
    }

    public class Fields1
    {
        public string Href { get; set; }
    }

    public class Resourcecontainers
    {
        public Collection Collection { get; set; }
        public Account Account { get; set; }
        public Project Project { get; set; }
    }

    public class Collection
    {
        public string Id { get; set; }
    }

    public class Account
    {
        public string Id { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
    }


}
