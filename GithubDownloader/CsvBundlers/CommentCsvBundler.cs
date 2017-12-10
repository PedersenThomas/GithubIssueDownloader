namespace GithubDownloader.CsvBundlers
{
    internal class CommentCsvBundler : CsvBundler<Comment>
    {
        public CommentCsvBundler(string folder, string outputFile) : base(folder, outputFile, Comment.CreateFromJObject)
        {
        }
    }
}