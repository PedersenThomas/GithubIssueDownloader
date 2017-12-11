namespace GithubDownloader.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using CsvHelper;
    using Model;
    using Newtonsoft.Json.Linq;

    internal class LabelsExtrater
    {
        public string EventsFolder { get; }
        public string OutputFile { get; }

        public LabelsExtrater(string eventsFolder, string outputFile)
        {
            this.EventsFolder = eventsFolder;
            this.OutputFile = outputFile;
        }

        public void GenerateFiles()
        {
            //Writes the CSV file with the labels.
            using (TextWriter writer = new StreamWriter(this.OutputFile))
            using (var csv = new CsvWriter(writer))
            {
                csv.Configuration.Encoding = Encoding.UTF8;

                //Using Reflection, writes the header.
                csv.WriteHeader<IssueLabeling>();

                var files = this.GetIssueLabeling();
                csv.WriteRecords(files);
            }
        }

        private IEnumerable<IssueLabeling> GetIssueLabeling()
        {
            var issueFolder = Directory.EnumerateDirectories(this.EventsFolder);

            foreach (var folder in issueFolder)
            {
                yield return this.CreateIssueLabeling(folder);
            }
        }

        public IssueLabeling CreateIssueLabeling(string folder)
        {
            IssueLabeling result = new IssueLabeling();
            if (int.TryParse(Path.GetFileName(folder), out int issueNumber))
            {
                result.IssueNumber = issueNumber;

                var eventFiles = Directory.EnumerateFiles(folder);
                foreach (var eventFile in eventFiles)
                {
                    var content = File.ReadAllText(eventFile);
                    var json = JObject.Parse(content);


                    var eventType = json["event"];
                    if (eventType != null && string.Equals(eventType.Value<string>(), "labeled",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var label = ExtractLabel(json);
                        result.MarkLabel(label);
                    }
                }
            }

            return result;
        }

        private static string ExtractLabel(JObject obj)
        {
            var labelNameObject = obj["label"]?["name"];
            if (labelNameObject != null)
            {
                var label = labelNameObject.Value<string>();
                return label;
            }

            return string.Empty;
        }
    }
}