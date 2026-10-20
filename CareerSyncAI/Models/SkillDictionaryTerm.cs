namespace CareerSyncAI.Models;

public class SkillDictionaryTerm : IEntity
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public bool IsQualification { get; set; } = false;
}
