namespace StockReporter
{
    public class StockLimits
    {
        public string Symbol { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public override string ToString()
        {
            return string.Format("Symbol: {0}, Min: {1}, Max: {2}", Symbol, Min, Max);
        }
    }
}
