using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DiaryScraperCore
{
    public class DiaryPostDto
    {
        [JsonProperty("author_username")]
        public string AuthorUsername { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        [JsonProperty("no_comments")]
        public string NoComments { get; set; }
        public string Access { get; set; }
        public string Title { get; set; }
        [JsonProperty("message_html")]
        public string MessageHtml { get; set; }
        [JsonProperty("dateline_date")]
        public string DatelineDate { get; set; }
        [JsonProperty("dateline_cdate")]
        public string DatelistCdate { get; set; }
        [JsonProperty("postid")]
        public string PostId { get; set; }

        [JsonProperty("access_list")]
        public List<string> AccessList { get; set; } = new List<string>();
        [JsonProperty("current_mood")]
        public string CurrentMood { get; set; }
        [JsonProperty("current_music")]
        public string CurrentMusic { get; set; }

        public List<DiaryCommentDto> Comments { get; set; } = new List<DiaryCommentDto>();
    }

    public class DiaryCommentDto
    {
        [JsonProperty("author_username")]
        public string AuthorUsername { get; set; }
        public string Dateline { get; set; }
        [JsonProperty("message_html")]
        public string MessageHtml { get; set; }
    }
}