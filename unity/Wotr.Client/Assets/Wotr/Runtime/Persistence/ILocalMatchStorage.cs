using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Recovery I/O only; credentials and authoritative snapshots never enter this store.
    public interface ILocalMatchStorage
    {
        string MatchId { get; }
        Task<T> Read<T>(string name) where T : class;
        Task Write<T>(string name, T data) where T : class;
        Task SaveCommand(string name, GameCommand command);
        Task CompleteCommand(string name, string commandId);
        Task MergeNotificationRead(NotificationReadRecord incoming);
    }
}
