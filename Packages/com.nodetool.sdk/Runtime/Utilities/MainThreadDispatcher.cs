using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Nodetool.SDK.Unity
{
    public sealed class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private readonly ConcurrentQueue<Action> _actions = new();

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[NodetoolDispatcher]");
                    _instance = go.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        public void Enqueue(Action action)
        {
            if (action == null)
            {
                return;
            }

            _actions.Enqueue(action);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
