using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using mshtml;
using X.GlodEyes.Collectors;
using XFCollection.HtmlAgilityPack;
using XFCollection.Http;

namespace XFCollection.Gov
{
    /// <summary>
    /// ShandongCollector
    /// </summary>
    public class ShandongCollector:WebRequestCollector<IResut, ShandongParameter>
    {

        private int _gatherDays;
        //private int _totalPages;
        private string _partUrl = "http://www.ccgp-shandong.gov.cn/sdgp2014/site/channelall.jsp?colcode=0302&subject=&pdate=&curpage=";
        private readonly Queue<string> _urlQueue = new Queue<string>();
        private readonly HttpHelper _httpHelper = new HttpHelper()
        {
            HttpEncoding = Encoding.GetEncoding("gb2312")
        };

        /// <summary>
        /// DefaultMovePageTimeSpan
        /// </summary>
        public override double DefaultMovePageTimeSpan { get; } = 0;

        /// <summary>
        /// 测试
        /// </summary>
        internal static void Test()
        {
            var parameter = new ShandongParameter()
            {
                GatherDays = "3"
            };



            //var parameter = new NormalParameter { Keyword = @"http://shop73144325.taobao.com"};

            TestHelp<ShandongCollector>(parameter);
        }


        /// <summary>
        /// InitFirstUrl
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override string InitFirstUrl(ShandongParameter param)
        {
            _gatherDays = int.Parse(param.GatherDays);
            string firstUrl = $"{_partUrl}1";
            
            InitUrlQueue(InitTotalPages(_httpHelper.GetHtmlByGet(firstUrl)));
            return _urlQueue.Count==0?null:_urlQueue.Dequeue();
        }


        /// <summary>
        /// ParseCurrentItems
        /// </summary>
        /// <returns></returns>
        protected override IResut[] ParseCurrentItems()
        {
            List<IResut> resultList = new List<IResut>();
            HtmlNode htmlNode = HtmlAgilityPackHelper.GetDocumentNodeByHtml(HtmlSource);
            HtmlNodeCollection htmlNodeCollection = htmlNode.SelectNodes("//td[@class='Font9']");
            foreach (HtmlNode node in htmlNodeCollection)
            {
                string url = node.SelectSingleNode("./a[@class='five']")?.Attributes["href"]?.Value;
                string dateTimeString = Regex.Match(node.InnerText, @"\d+-\d+-\d+").Value;
                if(string.IsNullOrEmpty(url)||string.IsNullOrEmpty(dateTimeString))
                    break;
                url = $"http://www.ccgp-shandong.gov.cn{url}";
                DateTime dateTime = Convert.ToDateTime(dateTimeString);
                int days = (DateTime.Now - dateTime).Days;
                if (days > _gatherDays)
                {
                    _urlQueue.Clear();
                    break;
                }
                string html = _httpHelper.GetHtmlByGet(url);
                HtmlNode htmlNode2 = HtmlAgilityPackHelper.GetDocumentNodeByHtml(html);
                string title = htmlNode2.SelectSingleNode("//div[@align='center']")?.InnerText;
                string publisher = Regex.Match(html, "(?<=发布人[:：]).*(?=</td>)").Value;
                string publishTime = Regex.Match(html, "(?<=发布时间[:：]).*(?=</td>)").Value;
                publishTime = Convert.ToDateTime(publishTime).ToString(CultureInfo.CurrentCulture);
                //string content = htmlNode2.SelectSingleNode("//td[@bgcolor='#FFFFFF' and @align='center' and not(@valign)]").InnerText.Trim();
                //content = HttpUtility.HtmlDecode(Regex.Match(content, @".*(?=\r\n)").Value);
                string content = htmlNode2.SelectSingleNode("//table//tr[2]/td[2]/table").OuterHtml;
                
                Resut resut = new Resut()
                {
                    ["url"] = url,
                    ["title"] = title,
                    ["content"] = content,
                    ["publisher"] = publisher,
                    ["publishTime"] = publishTime
                };

                resultList.Add(resut);
            }
            return resultList.ToArray();
        }

        /// <summary>
        /// ParseNextUrl
        /// </summary>
        /// <returns></returns>
        protected override string ParseNextUrl()
        {
            return _urlQueue.Count == 0 ? null : _urlQueue.Dequeue();
        }



        private int InitTotalPages(string html)
        {
            int totalPages = 0;
            string totalPagesString = Regex.Match(html, @"(?<=<font color='red'>1</font>/)\d+(?=</strong>页)").Value;
            int.TryParse(totalPagesString, out totalPages); 
            return totalPages;
        }



        private void InitUrlQueue(int totalPages)
        {
            for (int i = 0; i < totalPages; i++)
            {
                _urlQueue.Enqueue($"{_partUrl}{i+1}");
            }
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

    }
}
