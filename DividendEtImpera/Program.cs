using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DividendEtImpera
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var year))
            {
                Console.WriteLine("Expected year as argument. Please input it:");
                year = int.Parse(Console.ReadLine());
            }

            var dividends = GetDividends(year).Result;

            DisplayDividends(dividends);

            Console.ReadKey();
        }

        private static void DisplayDividends(Dictionary<string, double> dividends)
        {
            foreach (var kvp in dividends.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"{kvp.Key} {kvp.Value}%");
            }
        }

        static async Task<string[]> GetSymbols(int year)
        {
            using (var client = new HttpClient())
            {
                var companyListHtml = await client.GetStringAsync($"https://www.tradeville.eu/dividende/dividende-{year}");

                var tableOpeningTagIndex = companyListHtml.IndexOf("<table ");
                var tableClosingTagIndex = companyListHtml.IndexOf("</table>");
                var companiesTable = companyListHtml.Substring(tableOpeningTagIndex, tableClosingTagIndex - tableOpeningTagIndex + "</table>".Length);

                var doc = XDocument.Parse(companiesTable);
                return doc.Element("table")
                    .Elements("tr")
                    .Skip(1)
                    .Select(tr => tr.Descendants("a").Single().Value)
                    .ToArray();
            }
        }

        static async Task<Dictionary<string, double>> GetDividends(int year)
        {
            var symbols = await GetSymbols(year);

            var random = new Random();

            Dictionary<string, double> dividends = new Dictionary<string, double>();
            using (var client = new HttpClient())
            {
                for (var i = 0; i < symbols.Length; i++)
                {
                    await Task.Delay(random.Next(504, 1371));

                    var stockHtml = await client.GetStringAsync($"https://www.tradeville.eu/actiuni/actiuni-{symbols[i]}");

                    var yieldLabelIndex = stockHtml.IndexOf("Divid. yield:");
                    var spanOpeningTagIndex = stockHtml.IndexOf("<span", yieldLabelIndex);
                    var spanClosingTagIndex = stockHtml.IndexOf("</span>", yieldLabelIndex);
                    var yieldSpan = stockHtml.Substring(spanOpeningTagIndex, spanClosingTagIndex - spanOpeningTagIndex + "</span>".Length);

                    var doc = XDocument.Parse(yieldSpan);
                    var yieldText = doc.Element("span").Value.Replace('%', '\0');
                    dividends[symbols[i]] = yieldText.Equals("n/a", StringComparison.OrdinalIgnoreCase) ? 0 : double.Parse(yieldText);

                    if (i > 0)
                    {
                        ClearCurrentConsoleLine();
                    }
                    Console.Write($"{(int)((double)i / symbols.Length * 100)}% done");
                }
                ClearCurrentConsoleLine();
            }

            return dividends;
        }

        private static void ClearCurrentConsoleLine()
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
