using System.Collections;
using UnityEngine;

namespace DigitalTwin.Stations.Aligner
{
    /// <summary>
    /// 웨이퍼 얼라이너 제어 로직. 척(chuck)을 Y축으로 회전시켜 노치를 목표각으로 정렬한다.
    ///   - Jog: 수동 회전.  StartAlign: 목표각까지 자동 정렬(서보).
    ///   - StepAlign: 시간 비의존 1스텝(결정론적 검증용).
    /// 게임 셸/계약에 의존하지 않음 → 단독 동작·검증 가능.
    /// </summary>
    public class AlignerController : MonoBehaviour
    {
        public Transform chuck;              // 회전 척(웨이퍼 부모)
        public float spinSpeed = 180f;       // deg/s
        public float alignTolerance = 1.0f;  // deg
        public float targetAngle = 0f;       // 노치 목표각(deg)

        public float NotchAngle => chuck != null ? Normalize(chuck.localEulerAngles.y) : 0f;
        public float NotchError => Mathf.DeltaAngle(NotchAngle, targetAngle);  // 목표까지 갈 각(부호)
        public bool  IsAligned  => Mathf.Abs(NotchError) <= alignTolerance;
        public bool  Aligning   { get; private set; }

        Coroutine co;

        public void Jog(float deltaDeg)       { if (chuck) chuck.Rotate(0f, deltaDeg, 0f, Space.Self); }
        public void SetOrientation(float deg) { if (chuck) chuck.localRotation = Quaternion.Euler(0f, deg, 0f); }

        /// <summary>목표각을 향해 최대 maxDeg 만큼 1스텝 회전. 정렬되면 true.</summary>
        public bool StepAlign(float maxDeg)
        {
            if (chuck == null) return true;
            float err = NotchError;
            if (Mathf.Abs(err) <= alignTolerance) return true;
            chuck.Rotate(0f, Mathf.Clamp(err, -maxDeg, maxDeg), 0f, Space.Self);
            return false;
        }

        public void StartAlign() { if (co == null) co = StartCoroutine(AlignRoutine()); }
        public void StopAlign()  { if (co != null) StopCoroutine(co); co = null; Aligning = false; }

        IEnumerator AlignRoutine()
        {
            Aligning = true;
            while (!StepAlign(spinSpeed * Time.deltaTime)) yield return null;
            Aligning = false; co = null;
        }

        static float Normalize(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}
