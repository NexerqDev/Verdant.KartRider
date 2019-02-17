using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Verdant.KartRider
{
    public class KartGame
    {
        public NaverAccount Account;
        public string MainCharName;

        private string ngmPath;

        private HttpClient webClient => Account.WebClient;

        public KartGame(NaverAccount account)
        {
            Account = account;
        }

        public async Task Init()
        {
            // Find NGM location
            ngmPath = Tools.GetNgmPath();
            if (ngmPath == null)
                throw new VerdantException.GameNotFoundException();

            if (!(await getKart()))
                throw new VerdantException.ChannelingRequiredException();
        }

        public async Task Channel()
        {
            await channeling();
            Account.SaveCookies();

            if (!(await getKart()))
                throw new Exception("no");
        }

        private const string LAUNCH_LINE = "-dll:platform.nexon.com/NGM/Bin/NGMDll.dll:1 -locale:KR -mode:launch -game:73985:0 -token:'{0}' -passarg:'null' -timestamp:{1} -position:'naverWeb' -service:6 -architectureplatform:'none'";
        public async Task Start(bool firstTry = true)
        {
            // last minute cookies need to be get here - MSGENC and what not
            int ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; // timestamp: https://stackoverflow.com/questions/17632584/how-to-get-the-unix-timestamp-in-c-sharp

            // last min update sesh
            Debug.WriteLine("updating session");
            var sesRes = await webClient.GetAsync("https://sso.nexon.game.naver.com/Ajax/Default.aspx?_vb=UpdateSession");

            if (Account.Preloaded)
            {
                await Account.WebClient.GetAsync("http://nxgamechanneling.nexon.game.naver.com/login/loginproc.aspx?gamecode=589824");
            }

            string npp = Account.Cookies.GetCookies(new Uri("http://nexon.game.naver.com"))["NPP"].Value;

            Debug.WriteLine("launching");
            string args = String.Format(LAUNCH_LINE, npp, ts.ToString());
            var psi = new ProcessStartInfo(ngmPath, args);
            Process.Start(psi);
        }

        private async Task channeling()
        {
            await webClient.GetAsync("http://api.game.naver.com/js/jslib.nhn?gameId=P_PN000222&needReload=true");
            await webClient.GetAsync("http://kart.nexon.game.naver.com/main/index.aspx");
        }

        private Regex charRepRegex = new Regex("<p class=\"user_name\"><strong>(.+?)</strong>");
        private async Task<bool> getKart()
        {
            HttpResponseMessage res = await webClient.GetAsync("http://kart.nexon.game.naver.com/main/index.aspx");
            res.EnsureSuccessStatusCode();

            string data = await res.Content.ReadAsStringAsync();
            if (data.Contains("javascript:doLogin()"))
                return false;

            Match m = charRepRegex.Match(data);
            MainCharName = "(Unknown)";
            if (m.Success)
                MainCharName = m.Groups[1].Value;

            return true;
        }
    }
}
