using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StockReporter
{
    public class StockReporter
    {
        private const string YahooFinanceUrl = "https://query.yahooapis.com/v1/public/yql?q={0}&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=";
        private const string YqlQuery = "select symbol, PreviousClose from yahoo.finance.quotes where symbol in ({0})";
        private const string DefaultStockFile = "Stocks.json";

        private static string DefaultStockPath
        {
            get
            {
                string defaultDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(defaultDir, DefaultStockFile);
            }
        }

        private static List<StockLimits> GetStockLimits(string filePath = "")
        {
            if (filePath.Trim() == string.Empty)
            {
                filePath = DefaultStockPath;
            }

            return JsonConvert.DeserializeObject<List<StockLimits>>(File.ReadAllText(filePath));
        }

        private static string YqlQueryFromSymbols(IEnumerable<string> symbolsToPull)
        {
            StringBuilder sb = new StringBuilder();
            string symbols = null;

            foreach(var s in symbolsToPull)
            {
                sb.AppendFormat("\"{0}\", ",s);
            }

            symbols = sb.ToString();

            return string.Format(YqlQuery, symbols.Substring(0, symbols.Length - 2));
        }

        private static string YqlUrlFromSymbols(IEnumerable<string> symbolsToPull)
        {
            string yqlQuery = Uri.EscapeUriString(YqlQueryFromSymbols(symbolsToPull)).Replace(",", "%2C");
            return string.Format(YahooFinanceUrl, yqlQuery);
        }

        private static List<Stock> PullStocksFromUrl(string url)
        {
            WebRequest request = WebRequest.Create(url);
            string fullResponse;

            using (var sr = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                fullResponse = sr.ReadToEnd();
            }

            return PullQuotesFromJson(fullResponse);
        }

        private static List<Stock> PullQuotesFromJson(string json)
        {
            JObject o = JObject.Parse(json);
            JArray resultArray = (JArray)o.SelectToken("query.results.quote");
            return resultArray.ToObject<List<Stock>>();
        }

        private static List<Stock> PullStocksUnderMin(IList<Stock> stockList, IList<StockLimits> stockLimitList)
        {
            Dictionary<string, double> minLookup = stockLimitList.ToDictionary(k => k.Symbol.ToLower(), v => v.Min);

            return stockList.Where(s => s.PreviousClose < minLookup[s.Symbol]).ToList();
        }

        private static List<Stock> PullStocksOverMax(IList<Stock> stockList, IList<StockLimits> stockLimitList)
        {
            Dictionary<string, double> maxLookup = stockLimitList.ToDictionary(k => k.Symbol.ToLower(), v => v.Max);
            return stockList.Where(s => maxLookup[s.Symbol] < s.PreviousClose).ToList();
        }

        private static void Main(string[] args)
        {
            var stockLimitList = GetStockLimits(args.FirstOrDefault() ?? string.Empty);
            var yahooUrl = YqlUrlFromSymbols(stockLimitList.Select(s => s.Symbol));
            List<Stock> quotes = PullStocksFromUrl("https://query.yahooapis.com/v1/public/yql?q=select%20symbol%2C%20PreviousClose%20from%20yahoo.finance.quotes%20where%20symbol%20in%20(%22vti%22%2C%20%22vxus%22)&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=");
            List<Stock> stocksUnderMin = PullStocksUnderMin(quotes, stockLimitList);
            List<Stock> stocksOverMax = PullStocksOverMax(quotes, stockLimitList);

            Console.WriteLine("Stock rules");
            stockLimitList.ForEach(sl => Console.WriteLine(sl));
            Console.WriteLine();

            Console.WriteLine("Stocks under min");
            stocksUnderMin.ForEach(s => Console.WriteLine(s));
            Console.WriteLine();

            Console.WriteLine("Stocks over max");
            stocksOverMax.ForEach(s => Console.WriteLine(s));
            Console.WriteLine();

            Console.WriteLine("Please press any key to continue...");
            Console.ReadLine();
        }
    }
}
