using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using X.GlodEyes.Collectors;
using X.GlodEyes.Collectors.Specialized.JingDong;
using XFCollection.Http;

namespace XFCollection.Zhe800
{
    /// <summary>
    /// ProductCollecor
    /// </summary>
    public class ProductCollecor: WebRequestCollector<IResut, ProductParameter>
    {
        private int _realTotalPage;
        private int _searchPage;
        private readonly HttpHelper _httpHelper = new HttpHelper();
        private readonly Queue<string> _urlQueue = new Queue<string>();
        private string partUrl = "https://search.zhe800.com/search?";


        /// <summary>
        /// 测试
        /// </summary>
        internal static void Test()
        {
            var parameter = new ProductParameter()
            {
                KeyWord = "连衣裙",
                SearchPage = "3"
            };



            //var parameter = new NormalParameter { Keyword = @"http://shop73144325.taobao.com"};

            TestHelp<ProductCollecor>(parameter);
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

        private void InitTotalPage(string html)
        {
            var htmlNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(html);
            var htmlNodeCollection = htmlNode.SelectNodes("//span[@class='page']");
            var maxPage = 0;
            foreach (var node in htmlNodeCollection)
            {
                var curPageString = node.InnerText;
                int curPage;
                if (int.TryParse(curPageString, out curPage))
                    maxPage = maxPage < curPage ? curPage : maxPage;
            }
            _realTotalPage = maxPage<_searchPage?maxPage:_searchPage;
        }


        private void InitUrlQueue(string keyWord)
        {
            for (var i = 0; i < _realTotalPage; i++)
            {
                _urlQueue.Enqueue($"{partUrl}keyword={keyWord}&page={i+1}");
            }
        }

        /// <summary>
        /// InitFirstUrl
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override string InitFirstUrl(ProductParameter param)
        {
            //转换失败 则为0
            int.TryParse(param.SearchPage,out _searchPage);
            var keyWord = param.KeyWord;
            var url = $"{partUrl}keyword={keyWord}";
            var html = _httpHelper.GetHtmlByGet(url);
            InitTotalPage(html);
            InitUrlQueue(keyWord);
            return _urlQueue.Count == 0 ? null : _urlQueue.Dequeue();
        }


        /// <summary>
        /// ParseCurrentItems
        /// </summary>
        /// <returns></returns>
        protected override IResut[] ParseCurrentItems()
        {
            var resultList = new List<IResut>();

            var partHtml = Regex.Match(HtmlSource, @"<div id=""normal_dealbox""[\s\S]*?(?=<div class= ""page_div clear area page_bottom"">)").Value;
            var productNameCollection = Regex.Matches(partHtml, "(?<=title=\").*?(?=\")");
            var urlCollection = Regex.Matches(partHtml, @"(?<=<h3>\s*<a target=""_blank"" href="")[\S]*?(?="")");
            var priceCollection = Regex.Matches(partHtml, "(?<=<em><b>¥</b>).*?(?=</em>)");
            var maxPriceCollection = Regex.Matches(partHtml, "(?<=<del class=\"list_price\">￥).*?(?=</del>)");
            var count = productNameCollection.Count;
            if (count != urlCollection.Count || count != priceCollection.Count || count != maxPriceCollection.Count)
                throw new Exception("开始的条数不匹配");
            for (var i = 0; i < count; i++)
            {
                var resut = new Resut
                {
                    ["ProductName"] = productNameCollection[i].ToString(),
                    ["Url"] = urlCollection[i].ToString(),
                    //促销价格
                    ["Price"] = priceCollection[i].ToString(),
                    //最大价格
                    ["MaxPrice"] = maxPriceCollection[i].ToString()
                };
                resultList.Add(resut);
            }


            var jsonString = Regex.Match(HtmlSource, "(?<=window.setDeals = ){.*}(?=;)").Value;
            //.Replace(@"""\",@"\").Replace(@"}""",@"}").Replace(@"""{","{");
            var jObject = JObject.Parse(jsonString);
            var jArray = JArray.Parse(jObject["deals"].ToString());
            foreach (var jToken in jArray)
            {
                var urlName = jToken["url_name"].ToString();
                var id = jToken["id"].ToString();
                var title = jToken["title"].ToString();
                var wuxianPrice = jToken["wuxian_price"].ToString();
                var listPrice = jToken["list_price"].ToString();
                var resut = new Resut
                {
                    ["ProductName"] = title,
                    ["Url"] = $"//out.zhe800.com/ju/deal/{urlName}_{id}",
                    //促销价格
                    ["Price"] = wuxianPrice,
                    //最大价格
                    ["MaxPrice"] = listPrice
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
    }
}
