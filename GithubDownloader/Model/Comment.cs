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

        public static Comment CreateFromGithub(JObject jobject, long issueNumber)
        {
            return new Comment
            {
                IssueNumber = issueNumber,
                Id = jobject["id"].Value<long>(),
                CreatedAt = jobject["created_at"].Value<DateTime>(),
                UpdatedAt = jobject["updated_at"].Value<DateTime?>(),
                AuthorAssociation = jobject["author_association"].Value<string>(),
                Body = jobject["body"].Value<string>(),
                UserName = jobject["user"]["login"].Value<string>(),
                UserId = jobject["user"]["id"].Value<long>(),
                UserType = jobject["user"]["type"].Value<string>(),
                UserIsSiteAdmin = jobject["user"]["site_admin"].Value<bool>(),
            };
        }
    }
}