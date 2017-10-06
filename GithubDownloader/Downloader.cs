namespace GithubDownloader
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Model;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Downloader
    {
        public Logger Logger { get; }
        public Configuration Configuration { get; }

        private readonly Client client;

        public DateTime StartTime { get; set; }

        private const Formatting JsonFormatting = Formatting.None;

        public Downloader(Client client, Configuration configuration, Logger logger)
        {
            this.Logger = logger;
            this.client = client;

            this.Configuration = configuration;
        }

        public async Task DownloadForRepo(string repo)
        {
            this.StartTime = DateTime.UtcNow;

            var sinceText = string.IsNullOrEmpty(this.Configuration.Since) ? string.Empty : $"&since={this.Configuration.Since}";
            string repoIssuesUrl = $"https://api.github.com/repos/{repo}/issues?state=all{sinceText}";

            await this.DownloadIssues(repoIssuesUrl);

            this.Configuration.Since = this.StartTime.ToString("o");
            this.Configuration.Save();
        }

        private async Task DownloadIssues(string uri)
        {
            string nextLink = uri;

            while (!string.IsNullOrWhiteSpace(nextLink))
            {
                ClientResult response = await this.client.SendRequest(nextLink);

                this.SleepIfNeeded(response);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    JArray jArrayIssues = JArray.Parse(response.Content);
                    foreach (JToken issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject) issueObject;

                            var number = jObject["number"].Value<int>();
                            if (!String.IsNullOrEmpty(this.Configuration.DebugFolder))
                            {
                                File.WriteAllText(Path.Combine(this.Configuration.DebugFolder, $"issue_{number}.json"),
                                    jObject.ToString(JsonFormatting));
                            }
                            Issue issue = Issue.CreateFromGithub(jObject);

                            string filename = Path.Combine(this.Configuration.IssueFolder, $"{number}.json");

                            if (jObject.TryGetValue("comments_url", out JToken commentsUri))
                            {
                                await this.DownloadComments(commentsUri.Value<string>(), number);
                            }

                            if (jObject.TryGetValue("events_url", out JToken eventsUri))
                            {
                                await this.DownloadEvents(eventsUri.Value<string>(), number);
                            }

                            //milestone 709
                            //assignee
                            //assignees

                            string resultingJson = JsonConvert.SerializeObject(issue, JsonFormatting);
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

        private async Task DownloadComments(string uri, int issueNumber)
        {
            string nextLink = uri;

            while (!string.IsNullOrWhiteSpace(nextLink))
            {
                ClientResult response = await this.client.SendRequest(nextLink);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    JArray jArrayIssues = JArray.Parse(response.Content);
                    foreach (JToken issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject) issueObject;

                            var id = jObject["id"].Value<int>();
                            if (String.IsNullOrEmpty(this.Configuration.DebugFolder))
                            {
                                File.WriteAllText(Path.Combine(this.Configuration.DebugFolder, $"comment_{id}.json"),
                                    jObject.ToString(JsonFormatting));
                            }

                            string filename = Path.Combine(this.Configuration.CommentsFolder, $"{id}.json");

                            Comment comment = Comment.CreateFromGithub(jObject, issueNumber, response.Response.Headers.ETag?.Tag);
                            string resultingJson = JsonConvert.SerializeObject(comment, JsonFormatting);
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
            string nextLink = uri;

            while (!string.IsNullOrWhiteSpace(nextLink))
            {
                ClientResult response = await this.client.SendRequest(nextLink);

                if (response != null && response.Response?.IsSuccessStatusCode == true)
                {
                    JArray jArrayIssues = JArray.Parse(response.Content);
                    foreach (JToken issueObject in jArrayIssues)
                    {
                        if (issueObject.Type == JTokenType.Object)
                        {
                            var jObject = (JObject) issueObject;

                            var id = jObject["id"].Value<int>();
                            if (String.IsNullOrEmpty(this.Configuration.DebugFolder))
                            {
                                File.WriteAllText(Path.Combine(this.Configuration.DebugFolder, $"event_{id}.json"),
                                    jObject.ToString(JsonFormatting));
                            }

                            string filename = Path.Combine(this.Configuration.EventsFolder, $"{id}.json");

                            Event eventObject = Event.CreateFromGithub(jObject, issueNumber, response.Response.Headers.ETag?.Tag);
                            string resultingJson = JsonConvert.SerializeObject(eventObject, JsonFormatting);
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

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private void SleepIfNeeded(ClientResult result)
        {
            if (result.RatelimitRemaining < 5)
            {
                long epochNow = Convert.ToInt64((DateTime.Now - Epoch).TotalSeconds);
                var buffer = 10;
                long sleeptime = result.RatelimitReset - epochNow + buffer;
                Thread.Sleep((int) sleeptime * 1000);
            }
        }
    }
}