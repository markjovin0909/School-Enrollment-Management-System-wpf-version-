using System.Collections.Concurrent;
using System.Windows.Input;

namespace School_Management_System.Services
{
    internal static class SessionActivityMessageFilterCompatExtensions
    {
        private static readonly ConcurrentDictionary<SessionActivityMessageFilter, PreProcessInputEventHandler> Handlers = new();

        public static void Attach(this SessionActivityMessageFilter filter)
        {
            if (Handlers.ContainsKey(filter))
            {
                return;
            }

            PreProcessInputEventHandler handler = (_, args) =>
            {
                if (SessionContext.CurrentUser == null)
                {
                    return;
                }

                var input = args.StagingItem.Input;
                if (input is MouseEventArgs or KeyboardEventArgs)
                {
                    SessionContext.Touch();
                }
            };

            if (Handlers.TryAdd(filter, handler))
            {
                InputManager.Current.PreProcessInput += handler;
            }
        }

        public static void Detach(this SessionActivityMessageFilter filter)
        {
            if (!Handlers.TryRemove(filter, out var handler))
            {
                return;
            }

            InputManager.Current.PreProcessInput -= handler;
        }
    }
}
