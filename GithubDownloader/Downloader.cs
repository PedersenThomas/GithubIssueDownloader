using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubDownloader
{
    using System.IO;
    using System.Threading;
    using Model;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Downloader
    {
        public Logger Logger { get; }
        public string IssueFolder { get; set; }
        public string CommentsFolder { get; set; }
        public string EventssFolder { get; set; }
        public string DebugFolder { get; set; }

        private Client client;

        public DateTime StartTime { get; set; }

        private const Formatting JsonFormatting = Formatting.None;

        public Downloader(Client client, Configuration configuration, Logger logger)
        {
            this.Logger = logger;
            this.client = client;

            this.IssueFolder = configuration.IssueFolder;
            this.CommentsFolder = configuration.CommentsFolder;
            this.EventssFolder = configuration.EventsFolder;
            this.DebugFolder = configuration.DebugFolder;
        }

        public async Task DownloadForRepo(string repo)
        {
            this.StartTime = DateTime.Now;

            string repoIssuesUrl = $"https://api.github.com/repos/{repo}/issues?state=all";

            await this.DownloadIssues(repoIssuesUrl);
        }

        private async Task DownloadIssues(string uri)
        {
            var nextLink = uri;

            while (!String.IsNullOrWhiteSpace(nextLink))
            {
                var response = await this.client.SendRequest(nextLink);

                this.SleepIfNeeded(response);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    var jArrayIssues = JArray.Parse(response.Content);
                    foreach (var issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject) issueObject;


                            var number = jObject["number"].Value<int>();
                            File.WriteAllText(Path.Combine(this.DebugFolder, $"issue_{number}.json"), jObject.ToString(JsonFormatting));
                            var issue = Issue.CreateFromGithub(jObject);

                            var filename = Path.Combine(this.IssueFolder, $"{number}.json");

                            if (jObject.TryGetValue("comments_url", out var commentsUri))
                            {
                                await this.DownloadComments(commentsUri.Value<string>(), number);
                            }

                            if (jObject.TryGetValue("events_url", out var eventsUri))
                            {
                                await this.DownloadEvents(eventsUri.Value<string>(), number);
                            }

                            //milestone 709
                            //assignee
                            //assignees

                            var resultingJson = JsonConvert.SerializeObject(issue, JsonFormatting);
                            File.WriteAllText(filename, resultingJson);
                        }
                    }

                    if (response.Links?.TryGetValue("next", out nextLink) != true)
                    {
                        nextLink = string.Empty;
                    }
                }
                else
                {
                    nextLink = string.Empty;
                }
            }
        }

        //private JObject CleanObject(JObject obj)
        //{
        //    var propertyToRemove = new List<string>
        //    {
        //        "repository_url", "labels_url", "comments_url", "events_url", "html_url", "commit_url"
        //    };
        //    foreach (var nodeName in propertyToRemove)
        //    {
        //        if (obj.HasProperty(nodeName))
        //        {
        //            obj.Remove(nodeName);
        //        }
        //    }

        //    //Rearrange the user info.
        //    JToken userToken;
        //    if (obj.TryGetValue("user", out userToken) || obj.TryGetValue("actor", out userToken))
        //    {
        //        var userObject = (JObject)userToken;
        //        obj.Add("userName", userObject["login"]);
        //        obj.Add("userId", userObject["id"]);
        //        obj.Add("userType", userObject["type"]);
        //        obj.Add("userSiteAdmin", userObject["site_admin"]);

        //        obj.Remove("User");
        //    }

        //    return obj;
        //}

        private async Task DownloadComments(string uri, int issueNumber)
        {

            var nextLink = uri;

            while (!String.IsNullOrWhiteSpace(nextLink))
            {
                var response = await this.client.SendRequest(nextLink);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    var jArrayIssues = JArray.Parse(response.Content);
                    foreach (var issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject)issueObject;

                            var id = jObject["id"].Value<int>();
                            File.WriteAllText(Path.Combine(this.DebugFolder, $"comment_{id}.json"), jObject.ToString(JsonFormatting));

                            var filename = Path.Combine(this.CommentsFolder, $"{id}.json");

                            var comment = Comment.CreateFromGithub(jObject, issueNumber);
                            var resultingJson = JsonConvert.SerializeObject(comment, JsonFormatting);
                            File.WriteAllText(filename, resultingJson);
                        }
                    }

                    if (jArrayIssues.Count == 0 || response.Links?.TryGetValue("next", out nextLink) != true)
                    {
                        nextLink = string.Empty;
                    }
                }
                else
                {
                    nextLink = string.Empty;
                }

                this.SleepIfNeeded(response);
            }
        }

        private async Task DownloadEvents(string uri, int issueNumber)
        {
            var nextLink = uri;

            while (!String.IsNullOrWhiteSpace(nextLink))
            {
                var response = await this.client.SendRequest(nextLink);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    var jArrayIssues = JArray.Parse(response.Content);
                    foreach (var issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject)issueObject;

                            var id = jObject["id"].Value<int>();
                            File.WriteAllText(Path.Combine(this.DebugFolder, $"event_{id}.json"), jObject.ToString(JsonFormatting));

                            var filename = Path.Combine(this.EventssFolder, $"{id}.json");

                            var eventObject = Event.CreateFromGithub(jObject, issueNumber);
                            var resultingJson = JsonConvert.SerializeObject(eventObject, JsonFormatting);
                            File.WriteAllText(filename, resultingJson);
                        }
                    }

                    if (jArrayIssues.Count == 0 || response.Links?.TryGetValue("next", out nextLink) != true)
                    {
                        nextLink = string.Empty;
                    }
                }
                else
                {
                    nextLink = string.Empty;
                }

                this.SleepIfNeeded(response);
            }
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private void SleepIfNeeded(ClientResult result)
        {
            if (result.RatelimitRemaining < 5)
            {
                var epochNow = Convert.ToInt64((DateTime.Now - epoch).TotalSeconds);
                var buffer = 10;
                long sleeptime = result.RatelimitReset - epochNow + buffer;
                Thread.Sleep((int)sleeptime * 1000);
            }
        }
    }
}
