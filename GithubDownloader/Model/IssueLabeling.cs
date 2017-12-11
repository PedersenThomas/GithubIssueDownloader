namespace GithubDownloader.Model
{
    using System.Collections.Generic;

    public class IssueLabeling
    {
        public int IssueNumber { get; set; }

        public bool Accepted { get; set; }
        public bool Application { get; set; }
        public bool Bug { get; set; }
        public bool Bydesign { get; set; }
        public bool ClaAlreadySigned { get; set; }
        public bool ClaNotRequired { get; set; }
        public bool ClaRequired { get; set; }
        public bool ClaSigned { get; set; }
        public bool Delivered { get; set; }
        public bool Duplicate { get; set; }
        public bool Enhancement { get; set; }
        public bool EventRequest { get; set; }
        public bool External { get; set; }
        public bool FunctionExpose { get; set; }
        public bool IdentityManagement { get; set; }
        public bool Inprogress { get; set; }
        public bool InputNeeded { get; set; }
        public bool Investigate { get; set; }
        public bool Question { get; set; }
        public bool ShipsInFutureUpdate { get; set; }
        public bool Suggestion { get; set; }
        public bool V1Blocker { get; set; }
        public bool Wontfix { get; set; }

        public void MarkLabel(string name, bool value = true)
        {
            string normName = name.ToLowerInvariant();

            switch (normName)
            {
                case "accepted": this.Accepted = value; break;
                case "application": this.Application = value; break;
                case "bug": this.Bug = value; break;
                case "bydesign": this.Bydesign = value; break;
                case "cla-already-signed": this.ClaAlreadySigned = value; break;
                case "cla-not-required": this.ClaNotRequired = value; break;
                case "cla-required": this.ClaRequired = value; break;
                case "cla-signed": this.ClaSigned = value; break;
                case "delivered": this.Delivered = value; break;
                case "duplicate": this.Duplicate = value; break;
                case "enhancement": this.Enhancement = value; break;
                case "event-request": this.EventRequest = value; break;
                case "external": this.External = value; break;
                case "function-expose": this.FunctionExpose = value; break;
                case "identity-management": this.IdentityManagement = value; break;
                case "inprogress": this.Inprogress = value; break;
                case "input needed": this.InputNeeded = value; break;
                case "investigate": this.Investigate = value; break;
                case "question": this.Question = value; break;
                case "ships-in-future-update": this.ShipsInFutureUpdate = value; break;
                case "suggestion": this.Suggestion = value; break;
                case "v1-blocker": this.V1Blocker = value; break;
                case "wontfix": this.Wontfix = value; break;
            }
        }
    }
}