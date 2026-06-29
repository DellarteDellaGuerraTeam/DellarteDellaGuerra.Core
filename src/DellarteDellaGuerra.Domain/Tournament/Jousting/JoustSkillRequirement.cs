namespace DellarteDellaGuerra.Domain.Tournament.Jousting
{
    public class JoustSkillRequirement
    {
        public const string Riding = "Riding";
        public const string Polearm = "Polearm";

        public string Name { get; }
        public int Minimum { get; }

        public JoustSkillRequirement(string name, int minimum)
        {
            Name = name;
            Minimum = minimum;
        }

        public bool IsMet(int skillValue) => skillValue >= Minimum;
    }
}
