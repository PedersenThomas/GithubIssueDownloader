namespace GithubDownloader.Model
{
    using Newtonsoft.Json.Linq;

    public class Event
    {
        public long Id { get; set; }
        public string EventName { get; set; }
        public string CreatedAt { get; set; }
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string UserType { get; set; }
        public bool UserIsSiteAdmin { get; set; }
        public long IssueNumber { get; set; }
        public string CommitId { get; set; }

        public static Event CreateFromGithub(JObject jObject, long issueNumber)
        {
            return new Event
            {
                IssueNumber = issueNumber,
                Id = jObject["id"].Value<long>(),
                EventName = jObject["event"].Value<string>(),
                CreatedAt = jObject["created_at"].Value<string>(),
                UserName = jObject["actor"]["login"].Value<string>(),
                UserId = jObject["actor"]["id"].Value<long>(),
                UserType = jObject["actor"]["type"].Value<string>(),
                UserIsSiteAdmin = jObject["actor"]["site_admin"].Value<bool>(),
                CommitId = jObject["commit_id"].Value<string>(),
            };
        }
    }
}