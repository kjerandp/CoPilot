namespace CoPilot.ORM.Database.Commands.Options
{
    public class CreateOptions : ScriptOptions
    {
        public bool UseSequenceForPrimaryKeys { get; set; }
        public int KeySequenceStartAt { get; set; }
        public int KeySequenceIncrementBy { get; set; }

        public new static CreateOptions Default()
        {
            var options = new CreateOptions
            {
                Parameterize = true,
                UseSequenceForPrimaryKeys = true,
                KeySequenceStartAt = 1,
                KeySequenceIncrementBy = 1,
                UseNvar = true
            };
            return options;
        }
    }
}
