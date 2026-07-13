using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace DigitalTwin.Comms
{
    /// <summary>
    /// 백그라운드 통신 스레드 → Unity 메인 스레드로 작업을 마샬링.
    /// Unity API 는 메인 스레드 전용이므로, 수신 콜백은 Enqueue 로 넘기고
    /// 이 컴포넌트의 Update 에서 메인 스레드로 실행한다. (Phase 5b 통신에서 사용)
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        static MainThreadDispatcher instance;

        public static void Enqueue(Action a) { if (a != null) queue.Enqueue(a); }

        /// <summary>씬에 디스패처가 없으면 생성해 반환.</summary>
        public static MainThreadDispatcher Ensure()
        {
            if (instance != null) return instance;
            instance = FindAnyObjectByType<MainThreadDispatcher>();
            if (instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                instance = go.AddComponent<MainThreadDispatcher>();
            }
            return instance;
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
        }

        void Update()
        {
            while (queue.TryDequeue(out var a))
            {
                try { a(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
