namespace DominosApp.Util.Logger
{
    public interface ILogger<TService> where TService : class
    {
        void LogInfo(LogModel model);
        void LogDebug<TObject>(LogModel model, TObject? @object) where TObject : class;
        void LogWarning(LogModel model);
        void LogError(LogModel model);
        void LogCritical(LogModel model);
    }
}
