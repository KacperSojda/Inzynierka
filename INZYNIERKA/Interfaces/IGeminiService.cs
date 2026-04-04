namespace INZYNIERKA.Services
{
    public interface IGeminiService
    {
        Task<string> AskAsync(string question, string prompt);
    }
}