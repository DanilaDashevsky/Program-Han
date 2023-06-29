namespace ProgramHanTwo.Models
{
    public class Person
    {
            public int Id { get; set; }
            public string Email { get; set; }
            public string Lastname { get; set; }
            public string Name { get; set; }
            public string Photo { get; set; }
            public string Password { get; set; }
            public string AboutMyself { get; set; }
            public string Article { get; set; }
            public IFormFile MyFile { get; set; }
            public byte Evaluate { get; set; }
        public List<string> FirstArray { get; set; }
        public bool indicator = false;
        public bool showResult { get; set; }
        public int score { get; set; }
        public int reservScore { get; set; }
        public int level { get; set; }
        public int quantity { get; set; }
        public string result { get; set; }
        public List<Person> PersonHistory = new List<Person> { };
    }
}
