namespace INZYNIERKA.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> AskAsync(string question, string prompt);
    }
}