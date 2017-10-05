namespace GithubDownloader
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CsvHelper;
    using Model;
    using Newtonsoft.Json;

    internal class Program
    {
        public static void Main(string[] args)
        {
            Configuration configuration = Configuration.LoadFromPath("appsettings.json");
            StartDownloading(configuration).GetAwaiter().GetResult();
            BundleToCsv(configuration);
        }

        private static async Task StartDownloading(Configuration configuration)
        {
            var logger = new Logger();

            var client = new Client(configuration.Username, configuration.ApiKey, logger);

            var downloader = new Downloader(client, configuration, logger);

            await downloader.DownloadForRepo(configuration.Repository);
        }

        private static void BundleToCsv(Configuration configuration)
        {
            BundleToCsv<Issue>(configuration.IssueFolder, configuration.IssueCsvFile);
            BundleToCsv<Comment>(configuration.CommentsFolder, configuration.CommentsCsvFile);
            BundleToCsv<Event>(configuration.EventsFolder, configuration.EventsCsvFile);
        }

        private static void BundleToCsv<T>(string folder, string outfile)
        {
            IOrderedEnumerable<string> files = Directory.EnumerateFiles(folder)
                .OrderBy(filename => int.Parse(Path.GetFileNameWithoutExtension(filename)));

            using (TextWriter writer = new StreamWriter(outfile))
            using (var csv = new CsvWriter(writer))
            {
                csv.Configuration.Encoding = Encoding.UTF8;

                csv.WriteHeader<T>();

                foreach (string file in files)
                {
                    var issue = JsonConvert.DeserializeObject<T>(File.ReadAllText(file));

                    csv.WriteRecord(issue);
                }
            }
        }
    }
}