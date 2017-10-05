namespace GithubDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Client
    {
        public Logger Logger { get; }
        private readonly HttpClient httpClient = new HttpClient();

        public Client(string username, string apiKey, Logger logger)
        {
            this.Logger = logger;
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(apiKey))
            {
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{apiKey}")));

                this.httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                this.httpClient.DefaultRequestHeaders.Add("User-Agent", "IssueDownloader");
            }
        }

        public async Task<ClientResult> SendRequest(string uri)
        {
            HttpResponseMessage response = await this.httpClient.GetAsync(uri);

            var clientResult = new ClientResult(response);
            this.Logger.Write($"Remaining: {clientResult.RatelimitRemaining} Send request to: {uri}");

            if (!response.IsSuccessStatusCode)
            {
                return clientResult;
            }

            string responseContent = await response.Content.ReadAsStringAsync();

            clientResult.Content = responseContent;

            if (response.Headers.TryGetValues("Link", out IEnumerable<string> linkHeader) && linkHeader != null)
            {
                IList<string> enumerable = linkHeader as IList<string> ?? linkHeader.ToList();
                if (enumerable.Count == 1)
                {
                    Dictionary<string, string> links = ParseLinkHeader(enumerable.First());
                    clientResult.Links = links;
                }
            }

            return clientResult;
        }

        private static Dictionary<string, string> ParseLinkHeader(string linkHeader)
        {
            //<https://api.github.com/repositories/41881900/issues?page=2>; rel="next", <https://api.github.com/repositories/41881900/issues?page=173>; rel="last"
            var dictionary = new Dictionary<string, string>();

            string[] links = linkHeader.Split(',');
            foreach (string linkEntry in links)
            {
                var searcher = new Regex("<(.*)>; rel=\"(.*)\"");
                Match match = searcher.Match(linkEntry);
                if (match.Groups.Count == 3)
                {
                    dictionary.Add(match.Groups[2].Value, match.Groups[1].Value);
                }
            }

            return dictionary;
        }
    }
}