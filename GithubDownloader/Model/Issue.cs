namespace GithubDownloader.Model
{
    using System;
    using Newtonsoft.Json.Linq;

    internal class Issue
    {
        public long Id { get; set; }
        public long Number { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public bool Locked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string AuthorAssociation { get; set; }
        public string Body { get; set; }
        public string UserName { get; set; }
        public long UserId { get; set; }
        public string UserType { get; set; }
        public bool UserIsSiteAdmin { get; set; }
        public string Etag { get; set; }

        public static Issue CreateFromJObject(JObject jobject)
        {
            return new Issue
            {
                Id = jobject["id"].Value<long>(),
                Number = jobject["number"].Value<long>(),
                Title = jobject["title"].Value<string>(),
                UserName = jobject["user"]["login"].Value<string>(),
                UserId = jobject["user"]["id"].Value<long>(),
                UserType = jobject["user"]["type"].Value<string>(),
                UserIsSiteAdmin = jobject["user"]["site_admin"].Value<bool>(),
                State = jobject["state"].Value<string>(),
                Locked = jobject["locked"].Value<bool>(),
                CreatedAt = jobject["created_at"].Value<DateTime>(),
                UpdatedAt = jobject["updated_at"].Value<DateTime?>(),
                ClosedAt = jobject["closed_at"].Value<DateTime?>(),
                AuthorAssociation = jobject["author_association"].Value<string>(),
                Body = jobject["body"].Value<string>()
            };
        }
    }
}