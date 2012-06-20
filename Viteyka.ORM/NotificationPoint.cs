using System;
using System.Collections.Generic;

namespace Viteyka.ORM
{
    public class NotificationPoint
    {
        private static NotificationPoint _point = new NotificationPoint();
        private static List<Action<object, object>> _callbacks = new List<Action<object, object>>();

        public static NotificationPoint Instance { get { return _point; } }

        internal void Notify(object sender, object message)
        {
            Action<object, object>[] cached = null;
            lock (_callbacks)
                cached = _callbacks.ToArray();
            foreach (var action in cached)
                if (action != null)
                    action(sender, message);
        }

        public void RegisterCallback(Action<object, object> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            lock (_callbacks)
                if (!_callbacks.Contains(callback))
                    _callbacks.Add(callback);
        }
    }
}
