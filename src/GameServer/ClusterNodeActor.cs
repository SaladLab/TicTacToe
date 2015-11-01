using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using System.Linq;
using Domain.Interfaced;

namespace GameServer
{
    public class ClusterNodeMessage
    {
        public class NodeUp
        {
            public Address Address;
        }

        public class ActorUp
        {
            public IActorRef Actor;
            public Type Type;
        }

        public class ActorDown
        {
            public IActorRef Actor;
            public Type Type;
        }
    }

    public class ActorDiscoveryMessage
    {
        public class WatchActor
        {
            public Type Type;
        }

        public class UnwatchActor
        {
            public Type Type;
        }

        public class ActorUp
        {
            public IActorRef Actor;
            public Type Type;
        }

        public class ActorDown
        {
            public IActorRef Actor;
            public Type Type;
        }
    }

    public class ClusterNodeActor : ReceiveActor
    {
        protected Cluster Cluster = Cluster.Get(Context.System);

        private ClusterNodeContext _context;

        private List<IActorRef> _nodes = new List<IActorRef>();

        private class ActorItem
        {
            public bool IsLocal;
            public IActorRef Actor;
            public Type Type;
        }
        private List<ActorItem> _actorItems = new List<ActorItem>();

        private class WatchItem
        {
            public IActorRef Actor;
            public Type Type;
        }
        private List<WatchItem> _watchItems = new List<WatchItem>();

        public ClusterNodeActor(ClusterNodeContext context)
        {
            _context = context;
            Receive<ClusterEvent.MemberUp>(m => Handle(m));
            Receive<ClusterEvent.UnreachableMember>(m => Handle(m));
            Receive<ClusterNodeMessage.NodeUp>(m => Handle(m));
            Receive<ClusterNodeMessage.ActorUp>(m => Handle(m));
            Receive<ClusterNodeMessage.ActorDown>(m => Handle(m));
            Receive<ActorDiscoveryMessage.WatchActor>(m => Handle(m));
            Receive<ActorDiscoveryMessage.UnwatchActor>(m => Handle(m));
            Receive<ActorDiscoveryMessage.ActorUp>(m => Handle(m));
        }

        protected override void PreStart()
        {
            Cluster.Subscribe(Self, new[] { typeof(ClusterEvent.MemberUp) });
        }

        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        private void Handle(ClusterEvent.MemberUp m)
        {
            if (Cluster.SelfUniqueAddress == m.Member.UniqueAddress)
            {
                Console.WriteLine("It's Me! {0} Role: {1}", Cluster.SelfUniqueAddress, string.Join(", ", Cluster.SelfRoles));
            }
            else
            {
                // TODO: 이게 만약 도착하지 않으면 어떻하지? Cluster 는 Up 되었는데 cluster Node 가 아직 없는 경우
                Context.ActorSelection(m.Member.Address + "/user/cluster").Tell(
                   new ClusterNodeMessage.NodeUp
                   {
                       Address = Cluster.SelfAddress
                   });
            }
        }

        private void Handle(ClusterEvent.UnreachableMember m)
        {
            Console.WriteLine("**** UnreachableMember {0}", m.Member.Address);
        }

        private void Handle(ClusterNodeMessage.NodeUp m)
        {
            Console.WriteLine("**** NodeUp {0}", m.Address);

            // Node 에 등록

            _nodes.Add(Sender);

            // 해당 Node 에게 내가 가지고 있는 Actor 를 알려줌

            foreach (var item in _actorItems)
            {
                if (item.IsLocal)
                    Sender.Tell(new ClusterNodeMessage.ActorUp { Actor = item.Actor, Type = item.Type }, Self);
            }
        }

        private void Handle(ClusterNodeMessage.ActorUp m)
        {
            Console.WriteLine("**** ActorUp {0}", m.Actor.Path);

            NotifyActorUp(false, m.Actor, m.Type);
        }

        private void Handle(ClusterNodeMessage.ActorDown m)
        {
            Console.WriteLine("**** ActorDown {0}", m.Actor.Path);

            // Actor 목록에서 제거

            _actorItems.RemoveAll(a => a.IsLocal == false && a.Actor == m.Actor && a.Type == m.Type);
        }

        private void Handle(ActorDiscoveryMessage.WatchActor m)
        {
            Console.WriteLine("**** WatchActor {0}", m.Type);
            _watchItems.Add(new WatchItem { Actor = Sender, Type = m.Type });

            // 이미 들고 있던 것 알려줌

            foreach (var actor in _actorItems.Where(a => a.Type == m.Type))
            {
                Sender.Tell(new ActorDiscoveryMessage.ActorUp { Actor = actor.Actor, Type = actor.Type });
            }
        }

        private void Handle(ActorDiscoveryMessage.UnwatchActor m)
        {
            Console.WriteLine("**** UnwatchActor {0}", m.Type);
            _watchItems.RemoveAll(w => w.Actor == Sender && w.Type == m.Type);
        }

        private void Handle(ActorDiscoveryMessage.ActorUp m)
        {
            Console.WriteLine("**** ActorUp {0}", m.Type, m.Actor);

            NotifyActorUp(true, m.Actor, m.Type);

            // 다른 Node 에게 알려줌

            foreach (var node in _nodes)
            {
                node.Tell(new ClusterNodeMessage.ActorUp { Actor = m.Actor, Type = m.Type });
            }
        }

        private void NotifyActorUp(bool isLocal, IActorRef actor, Type type)
        {
            // Actor 목록에 등록

            _actorItems.Add(new ActorItem { IsLocal = isLocal, Actor = actor, Type = type });

            // Local Watch 대상에게 알려줌

            foreach (var watch in _watchItems.Where(w => w.Type == type))
            {
                watch.Actor.Tell(new ActorDiscoveryMessage.ActorUp { Actor = actor, Type = type });
            }

            // Context 에 저장
            // TODO: ClusterNodeActor 랑 ClusterDiscoveryActor 는 분리하자

            if (type == typeof(IUserDirectory))
                _context.UserDirectory = new UserDirectoryRef(actor);
            else if (type == typeof(IGameDirectory))
                _context.GameDirectory = new GameDirectoryRef(actor);
            else if (type == typeof(IGamePairMaker))
                _context.GamePairMaker = new GamePairMakerRef(actor);
        }
    }
}
