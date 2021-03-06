﻿using System;
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
        private const string YqlQuery = "select {0} from yahoo.finance.quotes where symbol in ({1})";
        private const string DefaultStockFile = "Stocks.json";

        private List<Stock> quotes;
        private List<StockLimits> stockLimits;
        
        private static string DefaultStockPath
        {
            get
            {
                string defaultDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(defaultDir, DefaultStockFile);
            }
        }

        public StockReporter(List<Stock> quotes, List<StockLimits> stockLimits)
        {
            this.quotes = quotes;
            this.stockLimits = stockLimits;
        }

        public static void PrintProperties(object o)
        {
            StringBuilder sb = new StringBuilder();
            string propertyMessage;

            foreach (var p in o.GetType().GetProperties())
            {
                sb.AppendFormat("{0}: {1}, ", p.Name, p.GetValue(o));
            }

            propertyMessage = sb.ToString();
            Console.WriteLine(propertyMessage.Substring(0, propertyMessage.Length-2));
        }

        public void ReportStocksUsingLimits()
        {
            HashSet<string> reportedSymbols = new HashSet<string>();

            Console.WriteLine("Stocks under Min");
            foreach(var stock in PullStocksUnderMin())
            {
                reportedSymbols.Add(stock.Symbol);
                PrintProperties(stock);
            }
            Console.WriteLine();

            Console.WriteLine("Stocks over Max");
            foreach (var stock in PullStocksOverMax())
            {
                reportedSymbols.Add(stock.Symbol);
                PrintProperties(stock);
            }
            Console.WriteLine();

            Console.WriteLine("Stocks within Range");
            foreach(var q in quotes.Where(q => !reportedSymbols.Contains(q.Symbol)))
            {
                PrintProperties(q);
            }
            Console.WriteLine();
        }

        public void ReportRules()
        {
            Console.WriteLine("Stock Limits");
            stockLimits.ForEach(sl => PrintProperties(sl));
            Console.WriteLine();
        }

        private static void Main(string[] args)
        {
            List<StockLimits> stockLimits = GetStockLimitsFromFile(args.FirstOrDefault() ?? string.Empty);
            string[] symbols = stockLimits.Select(sl => sl.Symbol).ToArray();
            string yahooUrl = YqlUrlFromSymbols(symbols);
            List<Stock> quotes = PullStocksFromUrl(yahooUrl);
            StockReporter sr = new StockReporter(quotes, stockLimits);

            sr.ReportRules();
            sr.ReportStocksUsingLimits();

            Console.WriteLine("Query used: ");
            Console.WriteLine("\t" + YqlQueryFromSymbols(symbols));
            Console.WriteLine("Please press any key to continue...");
            Console.ReadLine();
        }

        private static List<StockLimits> GetStockLimitsFromFile(string filePath = "")
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

            foreach (var s in symbolsToPull)
            {
                sb.AppendFormat("\"{0}\", ", s);
            }

            symbols = sb.ToString();

            return string.Format(YqlQuery, StockColumnsToPull(), symbols.Substring(0, symbols.Length - 2));
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

        private static string StockColumnsToPull()
        {
            StringBuilder sb = new StringBuilder();
            string message;

            foreach (var stockName in typeof(Stock).GetProperties().Select(p => p.Name))
            {
                sb.AppendFormat("{0},", stockName);
            }

            message = sb.ToString();
            return message.Substring(0, message.Length - 1);
        }

        private List<Stock> PullStocksUnderMin()
        {
            Dictionary<string, double> minLookup = stockLimits.ToDictionary(k => k.Symbol, v => v.Min);
            return quotes.Where(s => s.PreviousClose < minLookup[s.Symbol]).ToList();
        }

        private List<Stock> PullStocksOverMax()
        {
            Dictionary<string, double> maxLookup = stockLimits.ToDictionary(k => k.Symbol, v => v.Max);
            return quotes.Where(s => maxLookup[s.Symbol] < s.PreviousClose).ToList();
        }
    }
}
