namespace GithubDownloader.CsvBundlers
{
    using Model;

    internal class IssueCsvBundler : CsvBundler<Issue>
    {
        public IssueCsvBundler(string folder, string outputFile) : base(folder, outputFile, Issue.CreateFromJObject)
        {
        }
    }
}