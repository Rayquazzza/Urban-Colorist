using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{
    public struct GameEvent
    {
        static GameEvent e;

        public string EventName;
        public int IntParameter;
        public Vector2 Vector2Parameter;
        public Vector3 Vector3Parameter;
        public bool BoolParameter;
        public string StringParameter;

        public static void Trigger(string eventName, int intParameter = 0, Vector2 vector2Parameter = default(Vector2), Vector3 vector3Parameter = default(Vector3), bool boolParameter = false, string stringParameter = "")
        {
            e.EventName = eventName;
            e.IntParameter = intParameter;
            e.Vector2Parameter = vector2Parameter;
            e.Vector3Parameter = vector3Parameter;
            e.BoolParameter = boolParameter;
            e.StringParameter = stringParameter;
            EventBus.Publish(e);
        }
    }


    public static class EventBus
    {
        private static Dictionary<Type, List<IEventListenerBase>> _subscribersList;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeStatics()
        {
            _subscribersList = new Dictionary<Type, List<IEventListenerBase>>();
        }

        static EventBus()
        {
            _subscribersList = new Dictionary<Type, List<IEventListenerBase>>();
        }

        /// <summary>
        /// Adds a new subscriber to a certain event.
        /// </summary>
        /// <param name="listener">listener.</param>
        /// <typeparam name="Event">The event type.</typeparam>
        public static void Subscribe<Event>(IEventListener<Event> listener) where Event : struct
        {
            Type eventType = typeof(Event);

            if (!_subscribersList.ContainsKey(eventType))
            {
                _subscribersList[eventType] = new List<IEventListenerBase>();
            }

            if (!SubscriptionExists(eventType, listener))
            {
                _subscribersList[eventType].Add(listener);
            }
        }

        public static void Unsubscribe<Event>(IEventListener<Event> listener) where Event : struct
        {
            Type eventType = typeof(Event);

            List<IEventListenerBase> subscriberList = _subscribersList[eventType];


            for (int i = subscriberList.Count - 1; i >= 0; i--)
            {
                if (subscriberList[i] == listener)
                {
                    subscriberList.Remove(subscriberList[i]);

                    if (subscriberList.Count == 0)
                    {
                        _subscribersList.Remove(eventType);
                    }

                    return;
                }
            }
        }
        public static void Publish<Event>(Event newEvent) where Event : struct
        {
            List<IEventListenerBase> list;
            if (!_subscribersList.TryGetValue(typeof(Event), out list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as IEventListener<Event>).OnEvent(newEvent);
            }
        }

        private static bool SubscriptionExists(Type type, IEventListenerBase receiver)
        {
            List<IEventListenerBase> receivers;

            if (!_subscribersList.TryGetValue(type, out receivers)) return false;

            bool exists = false;

            for (int i = receivers.Count - 1; i >= 0; i--)
            {
                if (receivers[i] == receiver)
                {
                    exists = true;
                    break;
                }
            }

            return exists;
        }
    }

    public static class EventRegister
    {
        public delegate void Delegate<T>(T eventType);

        public static void EventStartListening<EventType>(this IEventListener<EventType> caller) where EventType : struct
        {
            EventBus.Subscribe<EventType>(caller);
        }

        public static void EventStopListening<EventType>(this IEventListener<EventType> caller) where EventType : struct
        {
            EventBus.Unsubscribe<EventType>(caller);
        }
    }


    public interface IEventListenerBase { };

    public interface IEventListener<T> : IEventListenerBase
    {
        void OnEvent(T eventType);
    }
}
