using System.Collections.Concurrent;
using MigrateClient.Interfaces.Background;

namespace MigrateClient.Services.Background
{
    public class TaskProgressService : ITaskProgressService
    {
        private readonly ConcurrentDictionary<Guid, int> _taskProgress;

        public TaskProgressService()
        {
            _taskProgress = new ConcurrentDictionary<Guid, int>();
        }

        public void RegisterTask(Guid taskId)
        {
            _taskProgress.TryAdd(taskId, 0);
        }

        public void UpdateProgress(Guid taskId, int progress)
        {
            _taskProgress.AddOrUpdate(taskId, progress, (id, oldValue) => progress);
        }

        public int GetProgress(Guid taskId)
        {
            return _taskProgress.TryGetValue(taskId, out int progress) ? progress : -1;
        }
    }
}
