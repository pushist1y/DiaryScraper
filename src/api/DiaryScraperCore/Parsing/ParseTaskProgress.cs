using System;

namespace DiaryScraperCore
{
    public class ParseTaskProgress : TaskProgress
    {
        public ParseTaskProgress() : base(ParseProgressNames.PostsProcessed, ParseProgressNames.PostsDiscovered)
        {
            Values[ParseProgressNames.CurrentFile] = "";
        }
    }




}