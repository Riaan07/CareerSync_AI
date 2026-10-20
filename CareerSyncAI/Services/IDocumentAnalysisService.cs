using CareerSyncAI.Models;

namespace CareerSyncAI.Services;

public interface IDocumentAnalysisService
{
    ExtractionResult ExtractTerms(string text);
    MatchCalculationResult CalculateMatch(CV cv, JobDescription jobDescription);
}
