using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;


/****************************************************************
 *      MaiJZ                                                   *
 *      20161209                                                *
 *      https://github.com/maijz128/github-update-checker       *
 *                                                              *
 ****************************************************************/

namespace PPPwn_Loader.Tools
{
    public class UpdateChecker
    {
        public interface IWebClient
        {
            void DownloadHtml(string url, Action<string> callback);
        }

        public IWebClient WebClient;
        public string UserOrOrgName;
        public string RepoName;
        public string CurrentVersion;

        private string _UpdateURL = "https://api.github.com/repos/{USER_OR_ORG}/{REPO_NAME}/releases/latest";
        private string _ReleasesURL = "https://github.com/{USER_OR_ORG}/{REPO_NAME}/releases";



        public UpdateChecker(string userOrOrgName, string repoName, string currentVersion)
        {
            this.UserOrOrgName = userOrOrgName;
            this.RepoName = repoName;
            this.CurrentVersion = currentVersion;
            this.WebClient = new MyWebClient();
        }


        public void CheckUpdate(Action<string, string> callback)
        {
            this.WebClient.DownloadHtml(GetUpdateURL(), (html) =>
            {
                LatestReleases latest = LatestReleases.GetLatestReleases(html);
                callback(latest.tag_name, latest.body);
            });
        }


        public void CheckUpdate(Action<LatestReleases> callback)
        {
            this.WebClient.DownloadHtml(GetUpdateURL(), (html) =>
            {
                LatestReleases latest = LatestReleases.GetLatestReleases(html);
                callback(latest);
            });
        }


        public void HasNewVersion(Action<bool> callback)
        {
            CheckUpdate((latest) =>
            {
                int result = VersionComparer.CompareVersion(latest.tag_name, this.CurrentVersion);
                callback(result > 0);
            });
        }

        public void GetNewVersion(Action<LatestReleases> callback)
        {
            CheckUpdate((latest) =>
            {
                int result = VersionComparer.CompareVersion(latest.tag_name, this.CurrentVersion);
                if (result > 0)
                {
                    callback(latest);
                }
                else
                {
                    callback(null);
                }
            });
        }


        public Object OpenBrowserToReleases()
        {
            string url = _ReleasesURL;
            url = url.Replace("{USER_OR_ORG}", this.UserOrOrgName);
            url = url.Replace("{REPO_NAME}", this.RepoName);
            return System.Diagnostics.Process.Start(url);
        }


        public string GetUpdateURL()
        {
            string result = _UpdateURL;
            result = result.Replace("{USER_OR_ORG}", this.UserOrOrgName);
            result = result.Replace("{REPO_NAME}", this.RepoName);
            return result;
        }


        public class VersionComparer
        {

            public static int CompareVersion(string target, string current)
            {
                var target_f = Filter(target);
                var current_f = Filter(current);
                var tsplit = target_f.Split('.');
                var csplit = current_f.Split('.');
                var len = (tsplit.Length > csplit.Length) ? tsplit.Length : csplit.Length;

                for (int i = 0; i < len; i++)
                {
                    int tvalue = 0;
                    int cvalue = 0;

                    if (i < tsplit.Length)
                    {
                        int.TryParse(tsplit[i], out tvalue);
                    }

                    if (i < csplit.Length)
                    {
                        int.TryParse(csplit[i], out cvalue);
                    }

                    if (tvalue != cvalue)
                    {
                        return tvalue - cvalue;
                    }
                }
                return 0;
            }


            public static string Filter(string version)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var c in version)
                {
                    bool condition = c >= '0' && c <= '9';
                    if (condition || c == '.')
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

        }


        public class LatestReleases
        {

            public string html_url { get; set; }
            public string tag_name { get; set; }
            public string name { get; set; }
            public string body { get; set; }
            public IList<Assets> assets { get; set; }
            public string tarball_url { get; set; }
            public string zipball_url { get; set; }


            public static LatestReleases GetLatestReleases(string html)
            {
                return JsonConvert.DeserializeObject<LatestReleases>(html);
            }


            public class Assets
            {
                public string name { get; set; }
                public string size { get; set; }
                public string download_count { get; set; }
                public string created_at { get; set; }
                public string updated_at { get; set; }
                public string browser_download_url { get; set; }
            }
        }


        public class MyWebClient : IWebClient
        {
            private const string UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36";


            public void DownloadHtml(string url, Action<string> callback)
            {
                WebClient webclient = CreateWebClient();
                webclient.DownloadStringCompleted += (sender, e) =>
                {
                    if (!e.Cancelled && e.Error == null)
                    {
                        callback(e.Result);
                    }
                    else
                    {
                        callback("获取内容失败！");
                    }
                };
                webclient.DownloadStringAsync(new Uri(url));
            }


            private WebClient CreateWebClient()
            {
                WebClient result = new WebClient();
                result.Headers.Add("User-Agent", UserAgent);
                result.Encoding = System.Text.Encoding.UTF8;
                // result.Proxy = new WebProxy("http://127.0.0.1", 1080);
                return result;
            }

        }

    }

}