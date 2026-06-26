using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dystopia.Networking
{
    public class PlayFabOperationQueue
    {
        private readonly Queue<Action<Action>> _queue  = new();
        private readonly MonoBehaviour         _runner;
        private bool                           _processing;
        private const float                    TimeoutSeconds = 30f;

        public int  PendingCount => _queue.Count;
        public bool IsProcessing => _processing;

        public PlayFabOperationQueue(MonoBehaviour runner)
        {
            _runner = runner;
        }

        // Enqueue a PlayFab operation. The lambda receives a done() callback
        // it MUST call exactly once when the PlayFab round-trip completes.
        public void Enqueue(Action<Action> operation)
        {
            _queue.Enqueue(operation);
            Debug.Log($"[PlayFabQueue] Enqueued. Pending: {_queue.Count}");
            if (!_processing) ProcessNext();
        }

        private void ProcessNext()
        {
            if (_queue.Count == 0) { _processing = false; return; }

            _processing = true;
            var op = _queue.Dequeue();

            bool fired = false;
            void Done()
            {
                if (fired) return;  // guard: only the first Done() call counts
                fired = true;
                Debug.Log($"[PlayFabQueue] Op done. Remaining: {_queue.Count}");
                ProcessNext();
            }

            _runner.StartCoroutine(TimeoutGuard(() => fired, Done));
            op(Done);
        }

        private IEnumerator TimeoutGuard(Func<bool> isDone, Action forceAdvance)
        {
            yield return new WaitForSecondsRealtime(TimeoutSeconds);
            if (!isDone())
            {
                Debug.LogError($"[PlayFabQueue] TIMEOUT — op ran >{TimeoutSeconds}s without a callback. Forcing advance.");
                forceAdvance();
            }
        }
    }
}
