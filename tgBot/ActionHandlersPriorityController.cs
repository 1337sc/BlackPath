using System;
using System.Collections.Generic;

namespace tgBot
{
    [Obsolete]
    public class ActionHandlersPriorityController
    {
        public delegate void UsePlayer(Player p);
        private readonly List<PriorActionHandler> Handlers = new List<PriorActionHandler>();

        public void AddHandler(UsePlayer action, int priority = 0, string name = "")
        {
            Handlers.Add(new PriorActionHandler(priority, action, name));
        }
        public void RemoveHandler(string name)
        {
            var currentHandler = Handlers.Find(x => x.Name == name);
            Handlers.Remove(currentHandler);
        }
        public void RemoveHandler(int priority)
        {
            var currentHandler = Handlers.Find(x => x.Priority == priority);
            Handlers.Remove(currentHandler);
        }
        public void RemoveHandlers(int priority)
        {
            Handlers.RemoveAll(x => x.Priority == priority);
        }
        public void DoActions(Player p)
        {
            Handlers.Sort((x, y) => x.Priority - y.Priority);
            foreach (var handler in Handlers)
            {
                handler.Act(p);
            }
        }
        class PriorActionHandler
        {
            public int Priority { get; set; }
            public string Name { get; set; }
            public event UsePlayer Action;

            public PriorActionHandler(int priority, UsePlayer action, string name = "")
            {
                Priority = priority;
                Name = name;
                Action += action;
            }
            public void Act(Player p)
            {
                Action.Invoke(p);
            }
        }
    }
}
