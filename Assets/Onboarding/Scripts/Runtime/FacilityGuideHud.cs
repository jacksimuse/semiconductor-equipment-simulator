using System.IO;
using UnityEngine;

namespace Onboarding
{
    public class FacilityGuideHud : MonoBehaviour
    {
        [System.Serializable]
        public struct TaskStep
        {
            public string zoneId;
            public string title;
            public string destination;
            [TextArea] public string guideText;
        }

        public string guideName = "한서윤 매니저";
        public TaskStep[] tasks;
        public float arrivalBannerSeconds = 3f;
        public bool showGuidePanel = true;
        public Texture2D playerPortrait;
        public Texture2D hanSeoYoonPortrait;
        public Texture2D parkDoHyunPortrait;
        public Texture2D leeJiHoonPortrait;
        public Texture2D choiMinAhPortrait;
        public Texture2D kimTaeJoonPortrait;

        int currentTaskIndex;
        int completedCount;
        bool showMissionPanel;
        bool summaryMode;
        int guidePage;
        Vector2 missionScroll;
        string lastEnteredZoneId;
        string currentZoneName = "Onboarding Lobby";
        string currentGuideText = "오늘은 회사 시설을 돌며 장비 교육 흐름을 확인합니다. 파란 동선을 따라 첫 목적지로 이동하세요.";
        string arrivalText;
        float arrivalUntil = -1f;

        GUIStyle panelStyle;
        GUIStyle headerStyle;
        GUIStyle bodyStyle;
        GUIStyle smallStyle;
        GUIStyle bannerStyle;
        GUIStyle buttonStyle;
        GUIStyle portraitStyle;
        GUIStyle speechStyle;
        GUIStyle nameplateStyle;

        public string CurrentDestination => HasTask(currentTaskIndex) ? tasks[currentTaskIndex].destination : "교육 동선 완료";

        void Awake()
        {
            LoadPortraitFallbacks();

            if (tasks == null || tasks.Length == 0)
                tasks = DefaultTasks();

            if (HasTask(0))
            {
                currentZoneName = tasks[0].destination;
                completedCount = 1;
                currentTaskIndex = Mathf.Min(1, tasks.Length);
                arrivalText = $"{tasks[0].destination}에 도착했습니다.";
                arrivalUntil = Time.time + arrivalBannerSeconds;
                currentGuideText = HasTask(currentTaskIndex)
                    ? tasks[currentTaskIndex].guideText
                    : tasks[0].guideText;
            }
        }

        public void EnterZone(FacilityGuideZone zone)
        {
            if (zone == null) return;

            bool sameZone = zone.zoneId == lastEnteredZoneId;
            bool advancedTask = false;
            currentZoneName = string.IsNullOrWhiteSpace(zone.displayName) ? zone.zoneId : zone.displayName;

            if (HasTask(currentTaskIndex) && zone.zoneId == tasks[currentTaskIndex].zoneId)
            {
                arrivalText = $"{tasks[currentTaskIndex].destination}에 도착했습니다.";
                arrivalUntil = Time.time + arrivalBannerSeconds;
                completedCount = Mathf.Max(completedCount, currentTaskIndex + 1);
                currentTaskIndex++;
                advancedTask = true;
            }

            if (HasTask(currentTaskIndex))
                currentGuideText = tasks[currentTaskIndex].guideText;
            else
                currentGuideText = "오늘의 기본 동선을 모두 확인했습니다. 고객 데모 전까지 원하는 공간을 다시 둘러보세요.";

            if (!sameZone || advancedTask)
                guidePage = 0;

            lastEnteredZoneId = zone.zoneId;
        }

        void OnGUI()
        {
            EnsureStyles();
            DrawArrivalBanner();
            DrawNotificationButton();
            if (showMissionPanel) DrawMissionPanel();
            if (showGuidePanel) DrawGuidePanel();
        }

        void DrawArrivalBanner()
        {
            if (Time.time > arrivalUntil || string.IsNullOrWhiteSpace(arrivalText)) return;

            float width = Mathf.Min(620f, Screen.width - 80f);
            var rect = new Rect((Screen.width - width) * 0.5f, 24f, width, 58f);
            GUI.Box(rect, $"● {arrivalText}", bannerStyle);
        }

        void DrawGuidePanel()
        {
            float width = Mathf.Min(1120f, Screen.width - 64f);
            float height = Mathf.Min(460f, Screen.height - 96f);
            height = Mathf.Max(height, Mathf.Min(360f, Screen.height - 48f));
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);

            const float buttonWidth = 86f;
            const float buttonHeight = 42f;
            const float buttonGap = 10f;
            float buttonY = rect.yMax - buttonHeight - 18f;

            var closeRect = new Rect(rect.xMax - buttonWidth - 20f, buttonY, buttonWidth, buttonHeight);
            if (GUI.Button(closeRect, "닫기", buttonStyle))
                showGuidePanel = false;

            var confirmRect = new Rect(closeRect.x - buttonWidth - buttonGap, buttonY, buttonWidth, buttonHeight);
            if (GUI.Button(confirmRect, "확인", buttonStyle))
                showGuidePanel = false;

            var nextRect = new Rect(confirmRect.x - buttonWidth - buttonGap, buttonY, buttonWidth, buttonHeight);
            string[] pages = BuildGuidePages();
            GUI.enabled = guidePage < pages.Length - 1;
            if (GUI.Button(nextRect, "다음", buttonStyle))
                guidePage = Mathf.Min(guidePage + 1, pages.Length - 1);
            GUI.enabled = true;

            var prevRect = new Rect(nextRect.x - buttonWidth - buttonGap, buttonY, buttonWidth, buttonHeight);
            GUI.enabled = guidePage > 0;
            if (GUI.Button(prevRect, "이전", buttonStyle))
                guidePage = Mathf.Max(guidePage - 1, 0);
            GUI.enabled = true;

            var summaryRect = new Rect(prevRect.x - buttonWidth - buttonGap, buttonY, buttonWidth, buttonHeight);
            if (GUI.Button(summaryRect, summaryMode ? "상세" : "요약", buttonStyle))
            {
                summaryMode = !summaryMode;
                guidePage = 0;
            }

            var portraitRect = new Rect(rect.x + 24f, rect.y + 24f, 210f, rect.height - 98f);
            DrawGuidePortrait(portraitRect);

            var namePlateRect = new Rect(portraitRect.x + 18f, portraitRect.yMax - 46f, portraitRect.width - 36f, 34f);
            GUI.Box(namePlateRect, ActiveGuideName(), nameplateStyle);

            var speechRect = new Rect(portraitRect.xMax + 24f, rect.y + 24f, rect.xMax - portraitRect.xMax - 48f, rect.height - 98f);
            GUI.Box(speechRect, GUIContent.none, speechStyle);
            GUI.Label(new Rect(speechRect.x + 24f, speechRect.y + 22f, speechRect.width - 48f, speechRect.height - 64f), pages[Mathf.Clamp(guidePage, 0, pages.Length - 1)], bodyStyle);
            GUI.Label(new Rect(speechRect.xMax - 120f, speechRect.yMax - 36f, 96f, 26f), $"{guidePage + 1}/{pages.Length}", smallStyle);
        }

        void DrawGuidePortrait(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, portraitStyle);

            Texture2D portrait = CurrentPortrait();
            if (portrait != null)
            {
                var imageRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 58f);
                GUI.DrawTexture(imageRect, portrait, ScaleMode.ScaleToFit, true);
                return;
            }

            Rect head = new Rect(rect.x + rect.width * 0.5f - 26f, rect.y + 28f, 52f, 52f);
            GUI.Box(head, "●", headerStyle);
            GUI.Label(new Rect(rect.x + 34f, rect.y + 86f, rect.width - 68f, 30f), "JSM", headerStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 122f, rect.width - 40f, 44f), "Training\nGuide", smallStyle);
        }

        Texture2D CurrentPortrait()
        {
            string activeName = ActiveGuideName();
            if (string.IsNullOrEmpty(activeName)) return hanSeoYoonPortrait;

            if (activeName.Contains("박도현")) return parkDoHyunPortrait != null ? parkDoHyunPortrait : hanSeoYoonPortrait;
            if (activeName.Contains("이지훈")) return leeJiHoonPortrait != null ? leeJiHoonPortrait : hanSeoYoonPortrait;
            if (activeName.Contains("최민아")) return choiMinAhPortrait != null ? choiMinAhPortrait : hanSeoYoonPortrait;
            if (activeName.Contains("김태준")) return kimTaeJoonPortrait != null ? kimTaeJoonPortrait : hanSeoYoonPortrait;
            if (activeName.Contains("플레이어")) return playerPortrait != null ? playerPortrait : hanSeoYoonPortrait;
            return hanSeoYoonPortrait;
        }

        string ActiveGuideName()
        {
            string zoneId = HasTask(currentTaskIndex) ? tasks[currentTaskIndex].zoneId : string.Empty;
            return zoneId switch
            {
                "cleanroom" => "최민아 책임",
                "maintenance" => "최민아 책임",
                "robotlab" => "박도현 책임",
                "control" => "이지훈 선임",
                "twinops" => "이지훈 선임",
                "demo" => "김태준 매니저",
                _ => string.IsNullOrEmpty(guideName) ? "한서윤 매니저" : guideName
            };
        }

        string[] BuildGuidePages()
        {
            if (summaryMode)
            {
                return new[]
                {
                    $"요약\n\n현재 위치: {currentZoneName}\n다음 목적지: {CurrentDestination}",
                    $"해야 할 일\n\n{CurrentTaskTitle()}\n\n핵심: {CurrentTaskSummary()}"
                };
            }

            return new[]
            {
                $"현재 위치: {currentZoneName}\n목적지: {CurrentDestination}",
                $"{currentGuideText}",
                $"{BuildTaskList()}"
            };
        }

        void DrawNotificationButton()
        {
            var rect = new Rect(Screen.width - 88f, 18f, 70f, 54f);
            if (GUI.Button(rect, "알림", buttonStyle))
            {
                showMissionPanel = !showMissionPanel;
                if (showMissionPanel) showGuidePanel = false;
            }
        }

        void DrawMissionPanel()
        {
            float width = Mathf.Min(620f, Screen.width - 48f);
            float height = Mathf.Min(430f, Screen.height - 128f);
            var rect = new Rect(Screen.width - width - 24f, 84f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);

            if (GUI.Button(new Rect(rect.xMax - 82f, rect.y + 12f, 64f, 34f), "닫기", buttonStyle))
                showMissionPanel = false;

            GUI.Label(new Rect(rect.x + 18f, rect.y + 16f, rect.width - 112f, 34f), "현재 진행중인 미션", headerStyle);

            string body = $"목적지: {CurrentDestination}\n현재 위치: {currentZoneName}\n해야 할 일: {CurrentTaskTitle()}\n\n상세 안내:\n{currentGuideText}\n\n핵심 요약:\n{CurrentTaskSummary()}\n\n{BuildTaskList()}";
            var viewRect = new Rect(rect.x + 18f, rect.y + 62f, rect.width - 36f, rect.height - 82f);
            float contentHeight = Mathf.Max(rect.height + 260f, 680f + tasks.Length * 32f);
            var contentRect = new Rect(0f, 0f, viewRect.width - 24f, contentHeight);
            missionScroll = GUI.BeginScrollView(viewRect, missionScroll, contentRect);
            GUI.Label(new Rect(0f, 0f, contentRect.width, contentRect.height), body, smallStyle);
            GUI.EndScrollView();
        }

        string BuildTaskList()
        {
            string text = "오늘의 할 일\n";
            for (int i = 0; i < tasks.Length; i++)
            {
                string mark = i < completedCount ? "■" : i == currentTaskIndex ? "▶" : "□";
                text += $"{mark} {tasks[i].title}\n";
            }
            return text;
        }

        string CurrentTaskTitle()
        {
            return HasTask(currentTaskIndex) ? tasks[currentTaskIndex].title : "교육 동선 완료";
        }

        string CurrentTaskSummary()
        {
            if (!HasTask(currentTaskIndex))
                return "고객 데모 전까지 필요한 공간을 다시 확인하세요.";

            return tasks[currentTaskIndex].zoneId switch
            {
                "gowning" => "클린룸 입장 준비를 완료합니다.",
                "cleanroom" => "파란 동선을 따라 안전하게 이동합니다.",
                "robotlab" => "장비 키오스크 앞에서 E로 제어 모드에 진입합니다.",
                "control" => "알람 발생 시 먼저 로그를 확인합니다.",
                "maintenance" => "E-STOP 복구 순서를 확인합니다.",
                "twinops" => "실장비/시뮬레이터 상태 동기화를 확인합니다.",
                "demo" => "교육 내용을 고객 데모 흐름으로 정리합니다.",
                _ => tasks[currentTaskIndex].guideText
            };
        }

        bool HasTask(int index)
        {
            return tasks != null && index >= 0 && index < tasks.Length;
        }

        void LoadPortraitFallbacks()
        {
            playerPortrait = LoadPortraitIfMissing(playerPortrait, "플레이어.png");
            hanSeoYoonPortrait = LoadPortraitIfMissing(hanSeoYoonPortrait, "한서윤.png");
            parkDoHyunPortrait = LoadPortraitIfMissing(parkDoHyunPortrait, "박도현.png");
            leeJiHoonPortrait = LoadPortraitIfMissing(leeJiHoonPortrait, "이지훈.png");
            choiMinAhPortrait = LoadPortraitIfMissing(choiMinAhPortrait, "최민아.png");
            kimTaeJoonPortrait = LoadPortraitIfMissing(kimTaeJoonPortrait, "김태준.png");
        }

        static Texture2D LoadPortraitIfMissing(Texture2D current, string fileName)
        {
            if (current != null) return current;

            string path = Path.Combine(Application.dataPath, fileName);
            if (!File.Exists(path)) return null;

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(fileName);
            return texture;
        }

        void EnsureStyles()
        {
            if (panelStyle != null) return;

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTex(new Color(0.02f, 0.30f, 0.44f, 0.90f));
            panelStyle.border = new RectOffset(12, 12, 12, 12);

            speechStyle = new GUIStyle(GUI.skin.box);
            speechStyle.normal.background = MakeTex(new Color(0.94f, 0.96f, 0.98f, 0.96f));
            speechStyle.border = new RectOffset(10, 10, 10, 10);

            portraitStyle = new GUIStyle(GUI.skin.box);
            portraitStyle.normal.background = MakeTex(new Color(0.88f, 0.90f, 0.92f, 0.96f));

            nameplateStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeTex(new Color(0.25f, 0.28f, 0.30f, 0.95f)) }
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                wordWrap = true,
                normal = { textColor = new Color(0.08f, 0.10f, 0.12f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.98f, 1f) }
            };

            bannerStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeTex(new Color(0.0f, 0.55f, 0.75f, 0.88f)) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
        }

        static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        static TaskStep[] DefaultTasks()
        {
            return new[]
            {
                new TaskStep { zoneId = "lobby", title = "출근 확인", destination = "Onboarding Lobby", guideText = "사원증과 오늘의 교육 목표를 확인하세요. 다음 목적지는 Gowning Area입니다." },
                new TaskStep { zoneId = "gowning", title = "클린룸 입장 준비", destination = "Gowning Area", guideText = "방진복 착용과 입장 체크를 완료하세요. 준비가 끝나면 Training Cleanroom으로 이동합니다." },
                new TaskStep { zoneId = "cleanroom", title = "클린룸 규칙 확인", destination = "Training Cleanroom", guideText = "파란 동선을 따라 이동하고 장비 상태등을 확인하세요. 다음은 Robot Transfer Lab입니다." },
                new TaskStep { zoneId = "robotlab", title = "이송 실습 구역 확인", destination = "Robot Transfer Lab", guideText = "FOUP, 로봇, 챔버 흐름을 확인하세요. 장비 키오스크 앞에서는 E로 제어 모드에 들어갑니다." },
                new TaskStep { zoneId = "control", title = "알람 로그 확인", destination = "Control Room", guideText = "장비가 멈추면 먼저 로그를 읽습니다. 무작정 Reset을 누르지 않는 것이 핵심입니다." },
                new TaskStep { zoneId = "maintenance", title = "안전 복구 절차", destination = "Maintenance Bay", guideText = "E-STOP과 인터락 복구 순서를 확인하세요. 장애물 제거, 안전 확인, Reset, Home 순서입니다." },
                new TaskStep { zoneId = "twinops", title = "트윈 상태 동기화", destination = "Twin Operations Room", guideText = "시뮬레이터와 장비 상태가 같은지 확인합니다. 통신 지연과 명령 실패도 여기서 해석합니다." },
                new TaskStep { zoneId = "demo", title = "고객 데모 리허설", destination = "Customer Demo Hall", guideText = "오늘 배운 장비 조작, 알람 대응, 트윈 상태 설명을 고객 데모 흐름으로 정리합니다." }
            };
        }
    }
}
