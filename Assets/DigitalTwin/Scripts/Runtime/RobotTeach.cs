using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// Phase 3 — 티칭 & 재생.
    ///   - Teach(): 현재 6축 각도를 웨이포인트로 저장.
    ///   - Play():  웨이포인트를 순서대로 조인트 공간 SmoothStep 보간으로 재생.
    ///   - Save/Load: 런타임 JSON (Application.persistentDataPath).
    /// 조인트 각도만 저장하므로 FK/IK 방식과 무관하게 재현 가능.
    /// </summary>
    [RequireComponent(typeof(SixAxisRobot))]
    public class RobotTeach : MonoBehaviour
    {
        [System.Serializable]
        public class Waypoint { public float[] angles; }

        [System.Serializable]
        class Program { public List<Waypoint> points = new List<Waypoint>(); }

        public List<Waypoint> waypoints = new List<Waypoint>();

        [Tooltip("웨이포인트 사이 이동 시간 (초)")]
        public float segmentTime = 1.5f;

        [Tooltip("마지막 → 처음으로 순환 재생")]
        public bool loop = false;

        public bool IsPlaying    { get; private set; }
        public int  CurrentIndex { get; private set; } = -1;

        SixAxisRobot robot;
        RobotIK      ik;
        Coroutine    playCo;

        void Awake()
        {
            robot = GetComponent<SixAxisRobot>();
            ik    = GetComponent<RobotIK>();
        }

        // ── 편집 ────────────────────────────────────────────────
        public void Teach()
        {
            if (robot == null) return;
            var a = new float[robot.joints.Length];
            for (int i = 0; i < a.Length; i++)
                a[i] = robot.joints[i] != null ? robot.joints[i].target : 0f;
            waypoints.Add(new Waypoint { angles = a });
        }

        public void DeleteLast()
        {
            if (waypoints.Count > 0) waypoints.RemoveAt(waypoints.Count - 1);
        }

        public void Clear()
        {
            Stop();
            waypoints.Clear();
        }

        // ── 재생 ────────────────────────────────────────────────
        public void Play()
        {
            if (robot == null || waypoints.Count < 2) return;
            Stop();
            if (ik != null) ik.follow = false;   // 재생 중 IK 추적 해제
            playCo = StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            if (playCo != null) StopCoroutine(playCo);
            playCo = null;
            IsPlaying = false;
            CurrentIndex = -1;
        }

        IEnumerator PlayRoutine()
        {
            IsPlaying = true;
            do
            {
                for (int seg = 0; seg < waypoints.Count - 1; seg++)
                {
                    CurrentIndex = seg;
                    yield return Interpolate(waypoints[seg].angles, waypoints[seg + 1].angles);
                }
            } while (loop);

            IsPlaying = false;
            CurrentIndex = -1;
        }

        IEnumerator Interpolate(float[] from, float[] to)
        {
            float dur = Mathf.Max(0.01f, segmentTime);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)); // ease in/out
                int n = Mathf.Min(robot.joints.Length, Mathf.Min(from.Length, to.Length));
                for (int i = 0; i < n; i++)
                    robot.SetJoint(i, Mathf.Lerp(from[i], to[i], s));
                yield return null;
            }
        }

        // ── 저장 / 불러오기 (JSON) ───────────────────────────────
        public string SavePath => Path.Combine(Application.persistentDataPath, "robot_program.json");

        public void SaveToDisk()
        {
            var prog = new Program { points = waypoints };
            File.WriteAllText(SavePath, JsonUtility.ToJson(prog, true));
            Debug.Log("[Teach] 저장 완료 (" + waypoints.Count + "개): " + SavePath);
        }

        public void LoadFromDisk()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[Teach] 저장 파일이 없습니다: " + SavePath);
                return;
            }
            Stop();
            var prog = JsonUtility.FromJson<Program>(File.ReadAllText(SavePath));
            waypoints = (prog != null && prog.points != null) ? prog.points : new List<Waypoint>();
            Debug.Log("[Teach] 불러오기 완료: " + waypoints.Count + "개");
        }
    }
}
