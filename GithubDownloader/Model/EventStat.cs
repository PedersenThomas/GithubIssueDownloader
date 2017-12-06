using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubDownloader.Model
{
    class EventStat
    {
        public int IssueNumber { get; set; }
        public string CreatedAt { get; set; }
        public string ResolvedAt { get; set; }
        internal IssueType IssueType { get; set; }
        public string IssueTypeText
        {
            get
            {
                return this.IssueType.ToString();
            }
        }
    }

    enum IssueType
    {
        None,
        Bug,
        ApplicationBug,
        ApplicationEnhancement,
        EventRequest
    }
}
