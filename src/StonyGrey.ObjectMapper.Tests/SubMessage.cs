namespace Tests
{
    public class SubMessage2
    {
        public string Value { get; set; }
    }

    public class SubMessage1
    {
        public string Value { get; set; }
        public SubMessage2 SubMessage2 { get; set; }
    }

    public class SubMessage
    {
        public string Value { get; set; }
        public SubMessage1 SubMessage1 { get; set; }
    }

    public class CollectionMessage
    {
        public string Value { get; set; }
        public IEnumerable<SubMessage> SubMessages { get; set; }
    }
}
