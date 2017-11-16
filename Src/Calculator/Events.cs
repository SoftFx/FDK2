namespace TickTrader.FDK.Calculator
{
    using System;

    public static class Events
    {
        public static void Raise(EventHandler eventHandler, object sender)
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, EventArgs.Empty);
        }

        public static void Raise<TEventArgs>(EventHandler<TEventArgs> eventHandler, object sender, Func<TEventArgs> argsFactory)
            where TEventArgs : EventArgs
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, argsFactory());
        }

        public static void Raise<TEventArgs>(EventHandler<TEventArgs> eventHandler, object sender, TEventArgs args)
            where TEventArgs : EventArgs
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, args);
        }
    }
}
