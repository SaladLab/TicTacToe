using Akka;
using Akka.Actor;
using Domain.Interfaced;

namespace GameServer
{
    public class ClusterNodeContext
    {
        public ActorSystem System;
        public IActorRef ClusterActorDiscovery;
        public IActorRef ClusterNodeContextUpdater;

        // quick access point for actors. but these are shared variables.
        // if there is a neat way to avoid this dirty hack, please improve it.
        public UserDirectoryRef UserDirectory;
        public GameDirectoryRef GameDirectory;
        public GamePairMakerRef GamePairMaker;
    }
}
