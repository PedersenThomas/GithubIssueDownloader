namespace GithubDownloader
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CsvHelper;
    using Model;
    using Newtonsoft.Json;
    using CsvHelper.TypeConversion;
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    internal class Program
    {
        public static void Main(string[] args)
        {
            Configuration configuration = Configuration.LoadFromPath("appsettings.json");
            //StartDownloading(configuration).GetAwaiter().GetResult();

            PostDownloadAnalysis(configuration);

            BundleToCsv(configuration);

            Console.WriteLine("Done!");
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
            var options = new TypeConverterOptions
            {
                Format = "o"
            };
            CsvHelper.TypeConversion.TypeConverterOptionsFactory.AddOptions<DateTime>(options);
            BundleToCsv<Issue>(configuration.IssueFolder, configuration.IssueCsvFile);
            BundleToCsv<Comment>(configuration.CommentsFolder, configuration.CommentsCsvFile);
            BundleToCsv<Event>(configuration.EventsFolder, configuration.EventsCsvFile);
            BundleToCsv<EventStat>(configuration.EventStatsFolder, configuration.EventStatsCsvFile);
        }

        private static void BundleToCsv<T>(string folder, string outfile)
        {
            IOrderedEnumerable<string> files = Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories)
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

        private static void PostDownloadAnalysis(Configuration configuration)
        {
            //This is a maga hack right now.
            if(!Directory.Exists(configuration.DebugFolder))
            {
                Console.WriteLine("No Debug folder, no post analysis");
                return;
            }

            var IssueFolder = Directory.EnumerateDirectories(configuration.EventsFolder);
            foreach (var folder in IssueFolder)
            {
                AnalysisIssueEvents(configuration, folder);
            }
        }

        private static void AnalysisIssueEvents(Configuration configuration, string absoluteEventsFolder)
        {
            int issueNumber;
            if (int.TryParse(Path.GetFileName(absoluteEventsFolder), out issueNumber))
            {
                var events = new List<string>();
                var eventFiles = Directory.EnumerateFiles(absoluteEventsFolder);
                foreach (var eventFile in eventFiles)
                {
                    events.Add(Path.GetFileName(eventFile));
                }
                var eventStat = AnalysisIssueEvent(configuration, events, issueNumber);

                if(eventStat != null)
                {
                    string filename = Path.Combine(configuration.EventStatsFolder, $"{issueNumber}.json");
                    string content = JsonConvert.SerializeObject(eventStat);
                    File.WriteAllText(filename, content);
                }
            }
        }

        private static EventStat AnalysisIssueEvent(Configuration configuration, List<string> events, int issueNumber)
        {
            var eventStat = new EventStat()
            {
                IssueNumber = issueNumber
            };

            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var eventNumber in events)
            {
                var filename = Path.Combine(configuration.DebugFolder, $"event_{eventNumber}");
                if(!File.Exists(filename))
                {
                    continue;
                }
                var content = File.ReadAllText(filename);
                var JsonObject = JObject.Parse(content);

                var eventType = JsonObject["event"];

                if (eventType != null &&  String.Equals(eventType.Value<string>(), "labeled", StringComparison.OrdinalIgnoreCase))
                {
                    var label = ExtractLabel(JsonObject);
                    labels.Add(label);
                }
                else if (eventType != null && String.Equals(eventType.Value<string>(), "unlabeled", StringComparison.OrdinalIgnoreCase))
                {
                    var label = ExtractLabel(JsonObject);
                    labels.Remove(label);
                }

                if (eventStat.IssueType == IssueType.None)
                {
                    eventStat.IssueType = ClassifyIssue(labels);
                }
            }

            return eventStat;
        }

        private static string ExtractLabel(JObject obj)
        {
            var labelObj = obj["label"];
            if (labelObj != null)
            {
                var labelNameObject = labelObj["name"];
                if (labelNameObject != null)
                {
                    var label = labelNameObject.Value<string>();
                    return label;
                }
            }
            return string.Empty;
        }

        private static IssueType ClassifyIssue(HashSet<string> labels)
        {            
            if(ContainsLabels(labels, "Bug", "Accepted"))
            {
                return IssueType.Bug;
            }
            else if (ContainsLabels(labels, "Accepted", "Application", "Enhancement"))
            {
                return IssueType.ApplicationEnhancement;
            }
            else if (ContainsLabels(labels, "Accepted", "Application"))
            {
                return IssueType.ApplicationEnhancement;
            }
            else if (ContainsLabels(labels, "Accepted", "event-request"))
            {
                return IssueType.EventRequest;
            }

            return IssueType.None;
        }

        private static bool ContainsLabels(HashSet<string> input, params string[] search)
        {
            return search.All(s => input.Contains(s));
        }
    }
}