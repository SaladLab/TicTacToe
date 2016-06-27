using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Akka.Interfaced.SlimSocket.Client
{
    public class ChannelEventDispatcher : MonoBehaviour
    {
        private static ChannelEventDispatcher s_instance;
        private static bool s_instanceExists;

        private static readonly List<Tuple<SendOrPostCallback, object>> s_posts =
            new List<Tuple<SendOrPostCallback, object>>();

        public static ChannelEventDispatcher Instance
        {
            get { return s_instance; }
        }

        public static bool TryInit()
        {
            if (s_instanceExists)
                return false;

            s_instanceExists = true;

            var go = new GameObject("_ChannelEventDispatcher");
            s_instance = go.AddComponent<ChannelEventDispatcher>();
            DontDestroyOnLoad(go);
            return true;
        }

        public static void Post(SendOrPostCallback callback, object state)
        {
            lock (s_posts)
            {
                s_posts.Add(Tuple.Create(callback, state));
            }
        }

        private void Awake()
        {
            if (s_instance)
            {
                DestroyImmediate(this);
            }
            else
            {
                s_instance = this;
                s_instanceExists = true;
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
                s_instanceExists = false;
            }
        }

        private void Update()
        {
            lock (s_posts)
            {
                foreach (var post in s_posts)
                {
                    post.Item1(post.Item2);
                }
                s_posts.Clear();
            }
        }
    }
}
