namespace GithubDownloader.CsvBundlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using CsvHelper;
    using Newtonsoft.Json.Linq;

    internal class CsvBundler<T> : ICsvBundler
    {
        public string Folder { get; }
        public string OutputFile { get; }
        public Func<JObject, T> Creator { get; }

        public CsvBundler(string folder, string outputFile, Func<JObject, T> creator)
        {
            this.Folder = folder;
            this.OutputFile = outputFile;
            this.Creator = creator;
        }

        public virtual void WriteFile()
        {
            using (TextWriter writer = new StreamWriter(this.OutputFile))
            using (var csv = new CsvWriter(writer))
            {
                csv.Configuration.Encoding = Encoding.UTF8;

                //Using Reflection, writes the header.
                csv.WriteHeader<T>();

                var files = this.EnumerateFiles();
                foreach (string file in files)
                {
                    var obj = JObject.Parse(File.ReadAllText(file));

                    var model = this.Creator(obj);
                    csv.WriteField(model);

                    //Flush the record
                    csv.NextRecord();
                }
            }
        }

        public virtual IEnumerable<string> EnumerateFiles()
        {
            return Directory.EnumerateFiles(this.Folder, "*", SearchOption.AllDirectories);
        }
    }
}