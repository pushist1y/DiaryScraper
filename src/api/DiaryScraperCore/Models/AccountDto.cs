using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public class AccountDto
    {
        [JsonProperty("userid")]
        public string UserId { get; set; } = string.Empty;
        [JsonProperty("username")]
        public string UserName { get; set; } = "";
        [JsonProperty("shortname")]
        public string ShortName { get; set; } = "";
        [JsonProperty("journal")]
        public string Journal { get; set; } = "";
        [JsonProperty("profile_access")]
        public string ProfileAccess { get; set; } = "0";
        [JsonProperty("journal_access")]
        public string JournalAcceses { get; set; } = "0";
        [JsonProperty("comment_access")]
        public string CommentAccess { get; set; } = "0";
        [JsonProperty("by_line")]
        public string ByLine { get; set; } = "";
        public string Birthday = "00-00-00";
        public string Sex { get; set; } = "";
        public string Education { get; set; } = "";
        public string Sfera { get; set; } = "";
        public string About { get; set; } = "";
        public string Timezone { get; set; } = "0";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        [JsonProperty("journal_title")]
        public string JournalTitle { get; set; } = "";
        public string Epigraph { get; set; } = "";
        public string Avatar { get; set; } = "";
        [JsonProperty("profile_list")]
        public List<string> ProfileList {get;set;} = new List<string>();
        [JsonProperty("journal_list")]
        public List<string> JournalList {get;set;} = new List<string>();
        [JsonProperty("comment_list")]
        public List<string> CommentList {get;set;} = new List<string>();
        [JsonProperty("white_list")]
        public List<string> WhiteList {get;set;} = new List<string>();
        [JsonProperty("black_list")]
        public List<string> BlackList {get;set;} = new List<string>();
        public List<string> Tags {get;set;} = new List<string>();
        public List<string> Favourites {get;set;} = new List<string>();
        public List<string> Readers {get;set;} = new List<string>();
        public List<string> Communities {get;set;} = new List<string>();
        public List<string> Members {get;set;} = new List<string>();
        public List<string> Owners {get;set;} = new List<string>();
        public List<string> Moderators {get;set;} = new List<string>();
    }
}