namespace GithubDownloader.CsvBundlers
{
    using Model;

    internal class EventCsvBundler : CsvBundler<Event>
    {
        public EventCsvBundler(string folder, string outputFile) : base(folder, outputFile, Event.CreateFromJObject)
        {
        }
    }
}