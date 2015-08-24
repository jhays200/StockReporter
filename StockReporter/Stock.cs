namespace StockReporter
{
    public class Stock
    {
        public double PreviousClose { get; set; }

        public string Symbol { get; set; }

        public override string ToString()
        {
            return string.Format("Symbol: {0}, PreviousClose: {1}", Symbol, PreviousClose);
        }
    }
}
