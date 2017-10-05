namespace GithubDownloader
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    public class Configuration
    {
        [JsonIgnore]
        public string ConfigurationFilename { get; set; }

        public string Repository { get; set; }
        public string Username { get; set; }
        public string ApiKey { get; set; }
        public string Since { get; set; }
        public string IssueFolder { get; set; }
        public string CommentsFolder { get; set; }
        public string EventsFolder { get; set; }
        public string DebugFolder { get; set; }
        public string IssueCsvFile { get; set; }
        public string CommentsCsvFile { get; set; }
        public string EventsCsvFile { get; set; }


        public static Configuration LoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The configuration couldn't be found at: {path}", nameof(path));
            }

            string rawContent = File.ReadAllText(path);
            var result = JsonConvert.DeserializeObject<Configuration>(rawContent);

            result.ConfigurationFilename = path;
            return result;
        }

        public void Save()
        {
            string rawContent = JsonConvert.SerializeObject(this);
            File.WriteAllText(this.ConfigurationFilename, rawContent);
        }
    }
}