namespace GithubDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    public class ClientResult
    {
        internal HttpResponseMessage Response;

        public ClientResult(HttpResponseMessage response)
        {
            this.Response = response;
        }

        public string Content { get; set; }
        public Dictionary<string, string> Links { get; set; }

        public int RatelimitRemaining
        {
            get
            {
                try
                {
                    this.Response.Headers.TryGetValues("X-Ratelimit-Remaining", out IEnumerable<string> values);
                    IList<string> enumerable = values as IList<string> ?? values.ToList();

                    if (enumerable.Count == 1)
                    {
                        string text = enumerable.First();
                        if (int.TryParse(text, out int count))
                        {
                            return count;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Trying to read the ratelimit remaining but got: " + e);
                }

                return -1;
            }
        }

        public long RatelimitReset
        {
            get
            {
                try
                {
                    this.Response.Headers.TryGetValues("X-Ratelimit-Reset", out IEnumerable<string> values);
                    IList<string> enumerable = values as IList<string> ?? values.ToList();

                    if (enumerable.Count == 1)
                    {
                        string text = enumerable.First();
                        if (long.TryParse(text, out long count))
                        {
                            return count;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Trying to read the ratelimit reset but got: " + e);
                }

                return -1;
            }
        }
    }
}