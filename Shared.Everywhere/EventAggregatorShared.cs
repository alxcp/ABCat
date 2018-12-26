using System;
using System.Windows.Threading;
using Caliburn.Micro;
using JetBrains.Annotations;
using Action = System.Action;

namespace Shared.Everywhere
{
    public class EventAggregatorShared : IEventAggregatorShared
    {
        [NotNull] private readonly IEventAggregator _aggregator = new EventAggregator();

        public Dispatcher Dispatcher { get; set; }

        public bool HandlerExistsFor(Type messageType)
        {
            return _aggregator.HandlerExistsFor(messageType);
        }

        public void Subscribe(object subscriber)
        {
            _aggregator.Subscribe(subscriber);
        }

        public void Unsubscribe(object subscriber)
        {
            _aggregator.Unsubscribe(subscriber);
        }

        public void Publish(object message, Action<Action> marshal)
        {
            _aggregator.Publish(message, marshal);
        }

        public void PublishOnUIThread(object message)
        {
            Dispatcher?.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(() => { _aggregator.Publish(message, action => action()); }));
        }
    }
}