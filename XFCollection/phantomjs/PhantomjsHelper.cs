using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using OpenQA.Selenium.PhantomJS;
using XFCollection.Http;

namespace XFCollection.Phantomjs
{
    class PhantomjsHelper
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            
            GetCookies("https://list.taobao.com/itemlist/default.htm?viewIndex=1&commend=all&atype=b&nick=jackjones%E5%AE%98%E6%96%B9%E6%97%97%E8%88%B0&style=list&same_info=1&tid=0&isnew=2&zk=all&_input_charset=utf-8");

        }


        public static ProcessStartInfo CreateConsoleAppStartInfo(
        string fileName,
        string arguments = null,
        string workingDirectory = null,
        Encoding encoding = null)
        {
            arguments = arguments ?? string.Empty;
            workingDirectory = workingDirectory ?? string.Empty;
            encoding = encoding ?? Encoding.Default;

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                ErrorDialog = false
            };

            return startInfo;
        }




        public static string GetCookies(string url)
        {


            var workingDirectory = Environment.CurrentDirectory;
            var appPath = Path.Combine(workingDirectory, "phantomjs.exe");
            //这里有转义的问题 把文件路径用双引号包围起来
            var arguments = $"--ignore-ssl-errors=yes \"{Path.Combine(workingDirectory, "getTaobaoCookie.js")}\" {url}";

            #region 启动进程


            var appStartInfo = CreateConsoleAppStartInfo(appPath, arguments, workingDirectory);
            appStartInfo.WindowStyle = ProcessWindowStyle.Normal;
            var process = Process.Start(appStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("启动设置命令失败");
            }

            process.WaitForExit();

            var info = process.StandardOutput.ReadToEnd();
            Console.WriteLine(info);

            var errorInfo = process.StandardError.ReadToEnd();
            Console.WriteLine(errorInfo);



            return info;
            //Process p = new Process();
            //p.StartInfo.FileName = appPath;
            //p.StartInfo.WorkingDirectory = workingDirectory;
            //p.StartInfo.Arguments = arguments;
            //p.StartInfo.Arguments = string.Format("--ignore-ssl-errors=yes --load-plugins=yes " +  Path.Combine(Environment.CurrentDirectory, "getTaobaoCookie.js"));
            //p.StartInfo.CreateNoWindow = true;
            //    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            //if (!p.Start())
            //    throw new Exception("无法启动Headless浏览器.");

            //p.WaitForExit();

            #endregion

        }

        /// <summary>
        /// GetHtml
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHtml(string url)
        {
            var userAgent =
                @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.108 Safari/537.36 2345Explorer/8.6.2.15784";
            var options = new PhantomJSOptions();
            options.AddAdditionalCapability(@"phantomjs.page.settings.userAgent", userAgent);

            using (var driver = new PhantomJSDriver(options))
            {
                var navigation = driver.Navigate();
                navigation.GoToUrl(url);
                return driver.PageSource;
            }
        }

        /// <summary>
        /// GetCookiesByUrl
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetCookiesByUrl(string url)
        {
            var userAgent =
                @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.108 Safari/537.36 2345Explorer/8.6.2.15784";
            var options = new PhantomJSOptions();
            options.AddAdditionalCapability(@"phantomjs.page.settings.userAgent", userAgent);

            using (var driver = new PhantomJSDriver(options))
            {
                var navigation = driver.Navigate();
                navigation.GoToUrl(url);
                var cookies = driver.Manage().Cookies.AllCookies;
                string cookiesString = "";
                bool isFirst = true;
                foreach (var cookie in cookies)
                {
                    if (isFirst)
                    {
                        cookiesString += HttpHelper.GetFormatCookies(cookie.ToString());
                        isFirst = false;
                    }
                    else
                    {
                        cookiesString = $"{cookiesString};{HttpHelper.GetFormatCookies(cookie.ToString())}";
                    }
                }
                Console.WriteLine($"cookies:{cookiesString}");
                return cookiesString;
            }
        }


        public void Test()
        {
            var html = GetHtml("http://wenshu.court.gov.cn/content/content?DocID=7ef23f41-b6c5-4c24-acfb-a74300f618d7");
            //var cookies = GetCookiesByUrl("http://wenshu.court.gov.cn/");
            //var cookies1 = GetCookiesByUrl("http://wenshu.court.gov.cn/content/content?DocID=7ef23f41-b6c5-4c24-acfb-a74300f618d7");
            //Console.WriteLine(html);
        }
            


    }
}
