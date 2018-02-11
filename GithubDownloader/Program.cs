namespace GithubDownloader
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using Analysis;
    using CsvBundlers;
    using CsvHelper.TypeConversion;
    using Newtonsoft.Json.Linq;
    using System.Diagnostics;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Configuration configuration = Configuration.LoadFromPath("appsettings.json");
            StartDownloading(configuration).GetAwaiter().GetResult();

            PostDownloadAnalysis(configuration);

            BundleToCsv(configuration);

            stopwatch.Stop();
            Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}.");
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
            CsvHelper.TypeConversion.TypeConverterOptionsFactory.AddOptions<DateTime?>(options);

            var bundlers = new List<ICsvBundler>
            {
                new IssueCsvBundler(configuration.IssueFolder, configuration.IssueCsvFile),
                new CommentCsvBundler(configuration.CommentsFolder, configuration.CommentsCsvFile),
                new EventCsvBundler(configuration.EventsFolder, configuration.EventsCsvFile)
            };

            foreach (ICsvBundler csvBundler in bundlers)
            {
                csvBundler.WriteFile();
            }
        }

        private static void PostDownloadAnalysis(Configuration configuration)
        {
            var extracter = new LabelsExtrater(configuration.EventsFolder, configuration.LabelsCsvFile);
            extracter.GenerateFiles();

            //var IssueFolder = Directory.EnumerateDirectories(configuration.EventsFolder);
            //foreach (var folder in IssueFolder)
            //{
            //    AnalysisIssueEvents(configuration, folder);
            //}
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
                var filename = ""; //Path.Combine(configuration.DebugFolder, $"event_{eventNumber}");
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