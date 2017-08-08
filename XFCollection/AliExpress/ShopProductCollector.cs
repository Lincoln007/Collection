using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using X.GlodEyes.Collectors;
using X.GlodEyes.Collectors.Specialized.JingDong;
using XFCollection.Http;

namespace XFCollection.AliExpress
{
    /// <summary>
    /// ShopProductCollector
    /// </summary>
    public class ShopProductCollector:WebRequestCollector<IResut,NormalParameter>
    {

        private string _url;
        private int _curPage;
        private string _shopId;
        private readonly HttpHelper _httpHelper;
        /// <summary>
        /// DefaultMovePageTimeSpan
        /// </summary>
        public override double DefaultMovePageTimeSpan => 10;

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public ShopProductCollector()
        {
            _httpHelper = new HttpHelper {Cookies = $"cna={GetCna()}"};
        }


        internal static void Test()
        {
            var parameter = new NormalParameter()
            {
                Keyword = "140011"
            };

            TestHelp<ShopProductCollector>(parameter);
        }

        /// <summary>
        /// InitFirstUrl
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        protected override string InitFirstUrl(NormalParameter param)
        {
            _shopId = param.Keyword;
            _url = $"https://www.aliexpress.com/store/all-wholesale-products/{_shopId}.html";
            //var html = _httpHelper.GetHtmlByGet(_url);
            _curPage = 0;
            return _url;
        }

        /// <summary>
        /// ParseCurrentItems
        /// </summary>
        /// <returns></returns>
        protected override IResut[] ParseCurrentItems()
        {

            var resultList = new List<IResut>();
            var html = _httpHelper.GetHtmlByGet(CurrentUrl);
            var documentNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(html);
            var htmlNodeCollection = documentNode.SelectNodes("//div[@class='ui-box-body']//li[@class='item']");
            if (htmlNodeCollection == null)
                return resultList.ToArray();

            foreach (var htmlNode in htmlNodeCollection)
            {
                IResut resut = new Resut();
                var productName = HttpUtility.HtmlDecode(htmlNode.SelectSingleNode(".//div[@class='detail']//a")?.InnerText??string.Empty);
                var productUrl = htmlNode.SelectSingleNode(".//div[@class='detail']//a")?.Attributes["href"].Value??string.Empty;
                var productId = Regex.Match(Regex.Match(productUrl, @"\d+_\d+").Value,@"(?<=_)\d+").Value;
                var price = Regex.Match(htmlNode.SelectSingleNode(".//b")?.InnerText??string.Empty,@"\d+\.?\d+").Value;
                var priceOld = Regex.Match(htmlNode.SelectSingleNode(".//del")?.InnerText??string.Empty, @"\d+\.?\d+").Value;
                var orderNum = Regex.Match(htmlNode.SelectSingleNode(".//div[@class='recent-order']")?.InnerText??string.Empty, @"\d+").Value;
                resut.Add("shopId",_shopId);
                resut.Add("productName", productName);
                resut.Add("productUrl", $"https:{productUrl}");
                resut.Add("productId",productId);
                resut.Add("price", FormatNumber(price));
                resut.Add("priceOld", FormatNumber(priceOld));
                resut.Add("orderNum", FormatNumber(orderNum));
                

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
            var documentNode = HtmlAgilityPack.HtmlAgilityPackHelper.GetDocumentNodeByHtml(HtmlSource);
            var href = documentNode.SelectSingleNode("//a[@class='ui-pagination-next']")?.Attributes["href"].Value??string.Empty;

            return string.IsNullOrEmpty(href) ? null : $"https:{href}";

        }

        /// <summary>
        /// FormatNumber
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private string FormatNumber(string number)
        {
            return string.IsNullOrEmpty(number) ? null : number;
        }

        /// <summary>
        /// ParseCurrentPage
        /// </summary>
        /// <returns></returns>
        protected override int ParseCurrentPage()
        {
            return ++_curPage;
        }


        /// <summary>
        /// UpdateResultRankInfo
        /// </summary>
        /// <param name="items"></param>
        /// <param name="page"></param>
        protected override void UpdateResultRankInfo(IResut[] items, int page)
        {
            base.UpdateResultRankInfo(items, page);

            foreach (var item in items)
            {
                item.Remove("SearchPageIndex");
                item.Remove("SearchPageRank");
            }


        }


        /// <summary>
        /// GetCna
        /// </summary>
        /// <returns></returns>
        private string GetCna()
        {
            var cna = string.Empty;
            var random = new Random();
            //abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ
            char[] array = {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
                'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
                'V', 'W', 'X', 'Y', 'Z',
                '/', '+'
            };
            for (var i = 0; i < 24; i++)
            {

                cna += array[random.Next(array.Length)];
            }

            return cna;
        }



    }
}
