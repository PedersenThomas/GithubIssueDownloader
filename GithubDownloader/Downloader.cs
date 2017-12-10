namespace GithubDownloader
{
    using System;
    using System.Globalization;
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
            this.CreateDirectories();

            this.StartTime = DateTime.UtcNow;

            var sinceText = string.IsNullOrEmpty(this.Configuration.Since) ? string.Empty : $"&since={this.Configuration.Since}";
            string repoIssuesUrl = $"https://api.github.com/repos/{repo}/issues?state=all{sinceText}";

            await this.DownloadIssues(repoIssuesUrl);

            this.Configuration.Since = this.StartTime.ToString("o");
            this.Configuration.Save();
        }

        public void CreateDirectories()
        {
            EnsureDirectoryCreated(this.Configuration.IssueFolder);
            EnsureDirectoryCreated(this.Configuration.CommentsFolder);
            EnsureDirectoryCreated(this.Configuration.EventsFolder);
            EnsureDirectoryCreated(this.Configuration.EventStatsFolder);
        }

        private static void EnsureDirectoryCreated(string folder)
        {
            if (!String.IsNullOrEmpty(folder) &&
                !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
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

                            if (jObject.TryGetValue("comments_url", out JToken commentsUri))
                            {
                                await this.DownloadComments(commentsUri.Value<string>(), number);
                            }

                            if (jObject.TryGetValue("events_url", out JToken eventsUri))
                            {
                                await this.DownloadEvents(eventsUri.Value<string>(), number);
                            }

                            string filename = Path.Combine(this.Configuration.IssueFolder, $"{number}.json");
                            File.WriteAllText(filename, jObject.ToString());
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
                            int id = jObject["id"].Value<int>();

                            string folder = Path.Combine(this.Configuration.CommentsFolder, issueNumber.ToString(CultureInfo.InvariantCulture));
                            EnsureDirectoryCreated(folder);

                            jObject.Add("issueNumber", issueNumber);
                            jObject.Add("etag", response.Etag);

                            string filename = Path.Combine(folder, $"{id}.json");
                            File.WriteAllText(filename, jObject.ToString());
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

                            string folder = Path.Combine(this.Configuration.EventsFolder, issueNumber.ToString(CultureInfo.InvariantCulture));
                            EnsureDirectoryCreated(folder);

                            jObject.Add("issueNumber", issueNumber);
                            jObject.Add("etag", response.Etag);

                            string filename = Path.Combine(folder, $"{id}.json");
                            File.WriteAllText(filename, jObject.ToString());
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