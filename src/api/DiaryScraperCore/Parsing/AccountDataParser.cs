using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class AccountDataParser
    {
        private HtmlParser _parser;
        private string _diaryDir;
        private ILogger _logger;
        public AccountDataParser(string diaryDir, ILogger logger)
        {
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
            _diaryDir = diaryDir;
            _logger = logger;
        }

        public async Task ParseAccountData(AccountDto accountDto, CancellationToken cancellationToken)
        {
            var methods = (from method in this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                           let parameters = method.GetParameters()
                           where parameters.Count() == 2
                           && parameters[0].ParameterType == typeof(AccountDto)
                           && parameters[1].ParameterType == typeof(CancellationToken)
                           && method.ReturnType == typeof(Task)
                           select method);

            var tasks = new List<Task>();
            try
            {
                foreach (var method in methods)
                {
                    var task = method.Invoke(this, new object[] { accountDto, cancellationToken }) as Task;
                    tasks.Add(task);
                }
                foreach (var task in tasks)
                {
                    await task;
                }
            }
            catch (AggregateException e)
            {
                foreach (var ee in e.InnerExceptions)
                {
                    _logger.LogError(ee, "Parsing error");
                }
            }
            catch (Exception e)
            {

                _logger.LogError(e, "Parsing error");
            }

            // this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            // .Where(m => m.GetParameters)
        }

        private async Task ParseMainPage(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.DiaryMain, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                var href = doc.QuerySelector("a[title='профиль']")?.GetAttribute("href") ??
                           doc.QuerySelector("#main_menu")?
                                .QuerySelectorAll("a")
                                .FirstOrDefault(a => a.HasAttribute("href") && a.GetAttribute("href").Contains("/member/?"))?
                                .GetAttribute("href");

                accountDto.UserId = Regex.Replace(href, @"\/member\/\?(\d+)$", "$1");
                //accountDto.UserName = profileLink.TextContent;

                var journalLink = doc.QuerySelector("#m_menu a") ?? doc.QuerySelector("#main_menu a");
                var journalMatch = Regex.Match(journalLink.GetAttribute("href"), @"https?:\/\/(.*)\.diary\.ru");
                if (journalLink.TextContent.Contains("ой дневник"))
                {
                    accountDto.Journal = "1";
                    if (journalMatch.Success)
                    {
                        accountDto.ShortName = journalMatch.Groups[1].Value;
                    }
                }
                else if (journalLink.TextContent.Contains("сообще"))
                {
                    accountDto.Journal = "2";
                    if (journalMatch.Success)
                    {
                        accountDto.ShortName = journalMatch.Groups[1].Value;
                    }
                }
                else if (journalLink.TextContent.Contains("авести"))
                {
                    accountDto.Journal = "0";
                }
            }
        }
        private async Task ParseMemberAccess(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.MemberAccess, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                var input = doc.QuerySelector("input[name='access_mode']:checked");
                accountDto.ProfileAccess = input.GetAttribute("value");
                accountDto.ProfileList.AddRange(doc.QuerySelector("#access_list").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
                accountDto.WhiteList.AddRange(doc.QuerySelector("#white_list").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private async Task<IHtmlDocument> GetDoc(string nameConst, CancellationToken cancellationToken)
        {
            var fileName = Path.Combine(_diaryDir, Constants.AccountPagesDir, nameConst);
            if (!File.Exists(fileName))
            {
                return null;
            }
            return await _parser.FromFileAsync(fileName, Encoding.GetEncoding(1251), cancellationToken);
        }

        private async Task ParsePch(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.DiaryPch, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                accountDto.BlackList.AddRange(doc.QuerySelector("#members").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private async Task ParseDiaryAccess(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.DiaryAccess, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                var accessInput = doc.QuerySelector("input[name='access_mode']:checked");
                accountDto.JournalAccess = accessInput.GetAttribute("value");

                var checked18 = doc.QuerySelector("input[name='access_mode2']");
                if (checked18.HasAttribute("checked"))
                {
                    accountDto.JournalAccess = Convert.ToString(Convert.ToInt32(accountDto.JournalAccess) + Convert.ToInt32(checked18.GetAttribute("value")));
                }
                accountDto.JournalList.AddRange(doc.QuerySelector("#access_list").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private async Task ParseCommentsAccess(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.DiaryCommentAccess, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                var accessInput = doc.QuerySelector("input[name='comments_access_mode']:checked");
                accountDto.CommentAccess = accessInput.GetAttribute("value");
                accountDto.CommentList.AddRange(doc.QuerySelector("#comments_access_list").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private async Task ParseMember(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.Member, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                var contant = doc.QuerySelector("#contant");
                if (contant == null)
                {
                    contant = doc.QuerySelector("#lm_right_content");
                }
                accountDto.UserName = contant.QuerySelector("b").TextContent;
                accountDto.Avatar = contant.QuerySelector("img").GetAttribute("src");
                foreach (var h6 in doc.QuerySelectorAll("h6"))
                {
                    var title = h6.ChildNodes.Where(n => n is IText).Select(n => n.TextContent).FirstOrDefault();
                    if (string.IsNullOrEmpty(title))
                    {
                        continue;
                    }
                    title = title.ToLower();
                    if (title.Contains("дневник:") || title.Contains("сообщество:"))
                    {
                        accountDto.JournalTitle = h6.QuerySelector("noindex").TextContent;
                    }
                    else if (title.Contains("участник сообществ:"))
                    {
                        accountDto.Communities.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (title.Contains("избранные дневники:"))
                    {
                        accountDto.Favourites.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (title.Contains("постоянные читатели:"))
                    {
                        accountDto.Readers.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (title.Contains("участники сообщества:"))
                    {
                        accountDto.Members.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (title.Contains("владельцы сообщества:"))
                    {
                        accountDto.Owners.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (title.Contains("модераторы сообщества:"))
                    {
                        accountDto.Moderators.AddRange(h6.NextElementSibling.QuerySelectorAll("a").Skip(1).Select(a => a.TextContent).Where(s => !string.IsNullOrEmpty(s)));
                    }
                }
            }
        }

        private async Task ParseTags(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.Tags, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                accountDto.Tags.AddRange(doc.QuerySelector("#textarea").TextContent.Split("\n").Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private async Task ParseProfile(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.Profile, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                accountDto.ByLine = doc.QuerySelector("input[name='usertitle']").GetAttribute("value");
                var month = doc.QuerySelector("select#month option[selected]")?.GetAttribute("value") ?? "0";
                var day = doc.QuerySelector("select#day option[selected]")?.GetAttribute("value") ?? "0";
                var year = doc.QuerySelector("select#year option[selected]")?.TextContent ?? "0";
                accountDto.Birthday = $"{year.PadLeft(4, '0')}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
                var sexVal = doc.QuerySelector("input[name='sex']:checked")?.GetAttribute("value");
                if (sexVal != null)
                {
                    accountDto.Sex = sexVal == "1" ? "Мужской" : "Женский";
                }
                accountDto.Education = doc.QuerySelector("select#education option[selected]")?.TextContent ?? "";
                accountDto.Sfera = doc.QuerySelector("select#sfera option[selected]")?.TextContent ?? "";
                accountDto.About = doc.QuerySelector("#about").TextContent;
            }
        }

        private async Task ParseGeo(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.Geography, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                accountDto.Country = doc.QuerySelector("select[name='country'] option[selected]")?.TextContent ?? "";
                accountDto.City = doc.QuerySelector("select[name='city'] option[selected]")?.TextContent ?? "";
                accountDto.City += doc.QuerySelector("input[name='other']")?.GetAttribute("value") ?? "";
                accountDto.Timezone = doc.QuerySelector("select[name='timezoneoffset'] option[selected]")?.GetAttribute("value") ?? "0";
            }
        }

        private async Task ParseEpigraph(AccountDto accountDto, CancellationToken cancellationToken)
        {
            using (var doc = await GetDoc(AccountPagesFileNames.Owner, cancellationToken))
            {
                if (doc == null)
                {
                    return;
                }
                accountDto.Epigraph = doc.QuerySelector("#message")?.TextContent ?? "";
            }
        }
    }
}