namespace GithubDownloader
{
    using System;
    using Newtonsoft.Json.Linq;

    public class Comment
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string UserType { get; set; }
        public bool UserIsSiteAdmin { get; set; }
        public long IssueNumber { get; set; }
        public string AuthorAssociation { get; set; }
        public string Etag { get; set; }

        public static Comment CreateFromJObject(JObject jObject)
        {
            return new Comment
            {
                IssueNumber = jObject["issueNumber"].Value<int>(),
                Etag = jObject["etag"].Value<string>(),
                Id = jObject["id"].Value<long>(),
                CreatedAt = jObject["created_at"].Value<DateTime>(),
                UpdatedAt = jObject["updated_at"].Value<DateTime?>(),
                AuthorAssociation = jObject["author_association"].Value<string>(),
                Body = jObject["body"].Value<string>(),
                UserName = jObject["user"]["login"].Value<string>(),
                UserId = jObject["user"]["id"].Value<long>(),
                UserType = jObject["user"]["type"].Value<string>(),
                UserIsSiteAdmin = jObject["user"]["site_admin"].Value<bool>()
            };
        }
    }
}