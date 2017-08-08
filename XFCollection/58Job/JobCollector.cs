using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using X.GlodEyes.Collectors;
using XFCollection.Http;


namespace XFCollection._58Job
{
    /// <summary>
    /// JobCollector
    /// </summary>
    public class JobCollector:WebRequestCollector<IResut, JobParameter>
    {
        private readonly Dictionary<string,string> _cityConvertDic = new Dictionary<string, string>();
        private readonly HttpHelper _httpHelper = new HttpHelper();

        /// <summary>
        /// DefaultMovePageTimeSpan
        /// </summary>
        //public override double DefaultMovePageTimeSpan { get; } = 30;

        private enum TimeEnum
        {
            不限 = 0,
            一天以内 = 1,
            三天以内 = 3,
            七天以内 = 7,
            十五天以内 = 15,
            一个月以内 = 30
        }




        /// <summary>
        /// 测试
        /// </summary>
        internal static void Test()
        {
            var parameter = new JobParameter()
            {
                City = "杭州",
                JobName = "淘宝客服",
                PublishTime = "一个月以内"
            };

            TestHelp<JobCollector>(parameter);
        }

        private void InitCityConvertDic()
        {
            
            const string url = "http://www.58.com/changecity.html";
            var html = GetWebContent(url);
            var content = Regex.Match(html, @"var cityList = {[\s\S]*}[\s]*(?=</script>)").Value;
            var matchCollection = Regex.Matches(content, "\"[^{]*?\":\".*?\"");
            foreach (Match match in matchCollection)
            {
                var vaule = match.Value;
                var city = Regex.Match(vaule, "(?<=^\")[^:]*?(?=\")").Value;
                var convertResult = Regex.Match(vaule, "(?<=:\").*?(?=\")").Value;
                _cityConvertDic.Add(city, convertResult);
            }
        }


        /// <summary>
        /// InitFirstUrl
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override string InitFirstUrl(JobParameter param)
        {
            //http://km.58.com/job/?key=%E6%B7%98%E5%AE%9D%E5%AE%A2%E6%9C%8D&final=1&jump=1&postdate=20170523_20170622
            //param = new JobParameter()
            //{
            //    City = "杭州",
            //    JobName = "淘宝客服",
            //    PublishTime = "一个月以内"
            //};

            InitCityConvertDic();

            var url = $"http://{_cityConvertDic[param.City]}.58.com/job/?key={param.JobName}&final=1&jump=1";

            //var jobName = "一个月以内";
            var timeEnum = (TimeEnum)Enum.Parse(typeof(TimeEnum), param.PublishTime);
            var timeInt = (int) timeEnum;
            if (timeInt != 0)
            {
                var timeEnd = int.Parse(DateTime.Now.ToString("yyyyMMdd")) + 1;
                var timeBegin = DateTime.Now.AddDays(1).AddDays(-timeInt).ToString("yyyyMMdd");
                var time = $"{timeBegin}_{timeEnd}";
                url = $"{url}&postdate={time}";
            }

            return url;
        }

        /// <summary>
        /// ParseCurrentItems
        /// </summary>
        /// <returns></returns>
        protected override IResut[] ParseCurrentItems()
        {
                  
            var resultList = new List<IResut>();
            //var curHtml = Phantomjs.PhantomjsHelper.GetHtml(CurrentUrl);
            //_httpHelper.Cookies = $"id58={GetId58()}";
            //var curHtml = _httpHelper.GetHtmlByGet(CurrentUrl);

            
            var htmlNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(HtmlSource);
            var htmlNodeCollection = htmlNode.SelectNodes("//div[@id='infolist']//dl");



            //一段时间会返回错误的结果 需要重试
            var tryTimes = 0;
            while (htmlNodeCollection == null)
            {
                if (++tryTimes > 3)
                    throw new Exception("htmlNodeCollectionNullException tryTimes more than 3 times");
                Console.WriteLine($"htmlNodeCollectionNullException tryTimes {tryTimes}");
                var html = GetWebContent(CurrentUrl);
                htmlNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(html);
                htmlNodeCollection = htmlNode.SelectNodes("//div[@id='infolist']//dl");
            }

            foreach (var node in htmlNodeCollection)
            {
                var jobUrl = node.SelectSingleNode("./dt/a")?.Attributes["href"].Value;
                if (string.IsNullOrEmpty(jobUrl))
                    throw new Exception("jobUrlNullException");
                var html = _httpHelper.GetHtmlByGet(jobUrl);
                var infoId = Regex.Match(html, @"(?<=""info[iI]d"":)\d+").Value;
                var userId = Regex.Match(html, @"(?<=""user[iI]d"":)\d+").Value;
                var statisticsHtml = _httpHelper.GetHtmlByGet($"http://statistics.zp.58.com/position/totalcount/?infoId={infoId}&userId={userId}");
                var htmlNodeR = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(html);
                var jobName = htmlNodeR.SelectSingleNode("//span[@class='pos_title']")?.InnerText;
                var jobCount = GetNumber(htmlNodeR.SelectSingleNode("//span[@class='item_condition pad_left_none']")?.InnerText);
                var degreeRequired = htmlNodeR.SelectSingleNode("//span[@class='item_condition']")?.InnerText;
                var experienceRequired = htmlNodeR.SelectSingleNode("//span[@class='item_condition border_right_None']")?.InnerText.Trim();
                var location = htmlNodeR.SelectSingleNode("//div[@class='pos-area']/span[1]")?.InnerText.Trim();
                var salary = htmlNodeR.SelectSingleNode("//span[@class='pos_salary']")?.InnerText?? htmlNodeR.SelectSingleNode("//span[@class='pos_salary daiding']").InnerText;
                var jobUpdateDate = FormatTime(Regex.Match(htmlNodeR.SelectSingleNode("//div[@class='pos_base_statistics']/span[1]")?.InnerText, "(?<=更新[:：]).*$").Value.Trim());
                //已找到
                _httpHelper.Referer = jobUrl;
                var browseCount = Regex.Match(_httpHelper.GetHtmlByGet($"http://jst1.58.com/counter?infoid={infoId}"), @"(?<=total=)\d+").Value;
                //已找到
                var applyCount = Regex.Match(statisticsHtml, @"(?<=""deliveryCount"":)\d+").Value;
                  
                var companyUrl = htmlNodeR.SelectSingleNode("//div[@class='baseInfo_link']/a")?.Attributes["href"].Value;
                
                var companyHtml = _httpHelper.GetHtmlByGet(companyUrl);
                var contactPerson = Regex.Match(companyHtml, @"(?<=<li><span>联系人.*</span>[\s]*)[\S]*?(?=[\s]*</li>)").Value;
                var phoneUrl = Regex.Match(companyHtml, @"(?<=<li><span>联系电话[\s\S]*?</span><img src="")[\S]*(?=""></li>)").Value;
                var phonePic = _httpHelper.GetImage(phoneUrl);
                var companyName = htmlNodeR.SelectSingleNode("//div[@class='baseInfo_link']/a")?.InnerText;
                //a[@class='comp_baseInfo_link']
                //已找到
                //http://zp.service.58.com/api?action=favorite,wltStats&params={"infoUrl":"http://hz.58.com/zptaobao/30334432354220x.shtml","userIds":"13663438612230_0"}
                var memberYearUrl = $"http://zp.service.58.com/api?action=favorite,wltStats&params={{\"infoUrl\":\"{ Regex.Match(jobUrl, @".*(?=\?)").Value}\",\"userIds\":\"{userId}_0\"}}";
                var memberYear = Regex.Match(_httpHelper.GetHtmlByGet(memberYearUrl), @"(?<=wlt)\d+").Value;
                var mainIndustry = htmlNodeR.SelectSingleNode("//a[@class='comp_baseInfo_link']")?.InnerText;
                var companyPersonCount = htmlNodeR.SelectSingleNode("//p[@class='comp_baseInfo_scale']")?.InnerText;
                var businessLicense = htmlNodeR.SelectSingleNode("//div[@class='identify_con clearfix']/span[1]")?.InnerText;
                var realNameLicense = htmlNodeR.SelectSingleNode("//div[@class='identify_con clearfix']/span[2]")?.InnerText;
                var taobaoShopLicense = htmlNodeR.SelectSingleNode("//div[@class='identify_con clearfix']/span[3]")?.InnerText;

                //已找到
                _httpHelper.Cookies = $"id58={GetId58()};58tj_uuid={Guid.NewGuid()}";
                var resumeFeedback = Regex.Match(_httpHelper.GetHtmlByGet($"http://jianli.58.com/ajax/getefrate/{userId}"), @"(?<=""efrate"":)\d+").Value;
                //已找到
                var companyJobNumber = Regex.Match(statisticsHtml, @"(?<=""infoCount"":)\d+").Value;
                var memberMonth = Regex.Match(htmlNodeR.SelectSingleNode("//span[@class='item_num join58_num']").InnerText,".*(?=月)").Value;
                var workAddress = htmlNodeR.SelectSingleNode("//div[@class='pos-area']/span[2]")?.InnerText;
                var jobDescription = htmlNodeR.SelectSingleNode("//div[@class='des']")?.InnerText;
                //var Phone_OCR

                var resut = new Resut
                {
                    ["JobUrl"] = jobUrl,
                    ["JobName"] = jobName,
                    ["JobCount"] = jobCount,
                    ["DegreeRequired"] = degreeRequired,
                    ["ExperienceRequired"] = experienceRequired,
                    ["Location"] = location,
                    ["Salary"] = salary,
                    ["JobUpdateDate"] = jobUpdateDate,
                    ["BrowseCount"] = browseCount,
                    ["ApplyCount"] = applyCount,
                    ["Phone_Pic"] = phonePic,
                    ["ContactPerson"] = contactPerson,
                    ["CompanyUrl"] = companyUrl,
                    ["CompanyName"] = companyName,
                    ["MemberYear"] = memberYear,
                    ["MainIndustry"] = mainIndustry,
                    ["CompanyPersonCount"] = companyPersonCount,
                    ["BusinessLicense"] = businessLicense,
                    ["TaobaoShopLicense"] = taobaoShopLicense,
                    ["RealNameLicense"] = realNameLicense,
                    ["ResumeFeedback"] = resumeFeedback,
                    ["CompanyJobNumber"] = companyJobNumber,
                    ["MemberMonth"] = memberMonth,
                    ["WorkAddress"] = workAddress,
                    ["JobDescription"] = jobDescription,
                    //["Phone_OCR"] = 
                };
                resultList.Add(resut);

            }

            return resultList.ToArray();
        }


        /// <summary>
        /// MoveNext
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            var result = base.MoveNext();

            var current = Current;
            if (current != null)
                Array.ForEach(current, item =>
                {
                    item.Remove(@"SearchPageIndex");
                    item.Remove(@"SearchPageRank");
                });

            return result;
        }

        /// <summary>
        /// GetMainWebContent
        /// </summary>
        /// <param name="nextUrl"></param>
        /// <param name="postData"></param>
        /// <param name="cookies"></param>
        /// <param name="currentUrl"></param>
        /// <returns></returns>
        protected override string GetMainWebContent(string nextUrl, byte[] postData, ref string cookies, string currentUrl)
        {
            return GetWebContent(nextUrl);
        }


        /// <summary>
        /// GetWebContent
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetWebContent(string url)
        {
            _httpHelper.Cookies = $"id58={GetId58()}";
            return _httpHelper.GetHtmlByGet(url);
        }

        /// <summary>
        /// ParseNextUrl
        /// </summary>
        /// <returns></returns>
        protected override string ParseNextUrl()
        {
            var htmlNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(HtmlSource);
            var url = htmlNode.SelectSingleNode("//a[@class='next']")?.Attributes["href"]?.Value;
            return url;
        }


        /// <summary>
        /// GetNumber
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected string GetNumber(string s)
        {
            return s == null ? null : Regex.Match(s, @"\d+").Value;
        }


        /// <summary>
        /// FormatTime
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private string FormatTime(string time)
        {
            if(time.Contains("小时")||time.Contains("今天"))
                return System.DateTime.Now.Date.ToString("yyyy-MM-dd");
            else if (time.Contains("天"))
            {
                var day = Regex.Match(time, @"\d+").Value;
                double dayDouble;
                if(double.TryParse(day,out dayDouble))
                    return System.DateTime.Now.AddDays(-double.Parse(day)).ToString("yyyy-MM-dd");
            }
            throw new Exception($"未知日期格式:{time}");
        }

        /// <summary>
        /// GetCna
        /// </summary>
        /// <returns></returns>
        private string GetId58()
        {
            var cna = string.Empty;
            var random = new Random();
            //abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
            var array = new[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
                'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
                'V', 'W', 'X', 'Y', 'Z',
                '/','='
            };
            for (var i = 0; i < 24; i++)
            {

                cna += array[random.Next(array.Length)];
            }

            return cna;
        }
    }
}
