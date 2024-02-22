namespace MigrateClient.Interfaces.Background
{
    public interface ITaskProgressService
    {
        void RegisterTask(Guid taskId);
        void UpdateProgress(Guid taskId, int progress);
        int GetProgress(Guid taskId);
    }
}
