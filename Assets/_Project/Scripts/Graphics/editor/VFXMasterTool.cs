using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// VFX Master Tool — VFXManager 전용 에디터 유틸리티
/// 
///
/// 기능:
///  - 등록된 VFX 프리팹 목록 표시 + 씬 프리뷰
///  - 카테고리별 전체 프리뷰 / 개별 프리뷰
///  - 프리뷰 스폰 위치 직접 지정 (XYZ / SceneView 카메라 앞)
///  - 프리뷰 중 파티클 색상 일괄 오버라이드
///  - 프리뷰 중 파티클 크기 배율 조절 (슬라이더)
///  - 파티클 수명 곡선 프리셋 적용 (팝업→즉시감소 / 선형증가 / 종형 / 일정유지)
///  - 프리뷰 인스턴스 자동 정리 (플레이모드 진입 시 포함)
///  - 풀 사이즈 인스펙터 편집
///  - 피격 플래시 / 디졸브 파라미터 실시간 편집
///  - 등록 누락 프리팹 경고
/// </summary>
public class VFXMasterTool : EditorWindow
{
    // ─── 상수 ───────────────────────────────────────
    private const float BUTTON_W = 72f;
    private const float PREVIEW_SEC = 3f;   // 프리뷰 자동 정리 딜레이(초)

    // ─── 카테고리 정의 ──────────────────────────────
    // (표시 이름, 필드명 배열) — VFXManager 필드명과 반드시 일치해야 함
    private static readonly (string label, string[] fields)[] Categories =
    {
        ("검 / 무기 이펙트",    new[] { "weaponSwingPrefab",     "weaponSkillPrefab"   }),
        ("타격 이펙트",         new[] { "attackHitSparkPrefab",  "attackImpactPrefab"  }),
        ("대시 이펙트",         new[] { "dashStartPrefab",       "dashTrailPrefab",    "dashEndPrefab" }),
        ("플레이어 피격/사망",  new[] { "playerHitPrefab",       "playerDeathPrefab"   }),
        ("몬스터 피격/사망",    new[] { "monsterHitPrefab",      "monsterDeathPrefab", "monsterDeathSmokePrefab" }),
    };

    // ─── 상태 ───────────────────────────────────────
    private VFXManager _manager;
    private SerializedObject _serialized;
    private Vector2 _scroll;

    private readonly List<GameObject> _previewInstances = new();
    private double _nextCleanupTime;

    private bool _showSettings = false;
    private bool _showMissing = true;
    private bool _showPreviewOptions = true;

    // ─── 프리뷰 옵션 ────────────────────────────────
    private Vector3 _previewPosition = Vector3.zero;   // 스폰 위치
    private bool _useColorOverride = false;           // 색상 오버라이드 활성화
    private Color _overrideColor = Color.white;     // 오버라이드 색상
    private bool _lockToSceneView = false;           // SceneView 카메라 앞에 스폰

    // ─── 크기 / 곡선 옵션 ───────────────────────────
    private bool _useSizeOverride = false;
    private float _sizeMultiplier = 1f;                // 크기 배율 (0.1 ~ 5.0)

    private bool _useCurveOverride = false;
    private CurvePreset _selectedCurve = CurvePreset.Constant;

    private enum CurvePreset
    {
        Constant,    // 일정 유지
        PopBurst,    // 팝업→즉시감소  (히트 스파크 느낌)
        LinearGrow,  // 선형 증가       (차징 느낌)
        Bell,        // 종형            (부드러운 등장/퇴장)
    }

    // ─── 열기 ───────────────────────────────────────
    [MenuItem("Tools/VFX Master Tool")]
    public static void Open() => GetWindow<VFXMasterTool>("VFX Master Tool");

    // ─── 라이프사이클 ───────────────────────────────
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        TryFindManager();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        ClearAllPreviews();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 플레이모드 진입 직전에 프리뷰 전부 정리
        if (state == PlayModeStateChange.ExitingEditMode)
            ClearAllPreviews();
    }

    // ─── 매니저 탐색 ────────────────────────────────
    private void TryFindManager()
    {
        // FindObjectOfType은 OnGUI가 아닌 여기서만 호출
        _manager = FindObjectOfType<VFXManager>();
        _serialized = _manager != null ? new SerializedObject(_manager) : null;
    }

    // ─── GUI 진입점 ─────────────────────────────────
    private void OnGUI()
    {
        // 프리뷰 자동 정리 타이머
        if (_previewInstances.Count > 0 && EditorApplication.timeSinceStartup > _nextCleanupTime)
            ClearAllPreviews();

        DrawToolbar();

        if (_manager == null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "씬에 VFXManager 오브젝트가 없습니다.\n" +
                "빈 오브젝트를 만들고 VFXManager 스크립트를 붙인 뒤 새로고침 버튼을 눌러주세요.",
                MessageType.Error);

            if (GUILayout.Button("씬에서 다시 찾기", GUILayout.Height(30)))
                TryFindManager();
            return;
        }

        // SerializedObject 동기화
        _serialized.Update();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawMissingWarnings();
        DrawPreviewOptions();
        DrawCategories();
        DrawSettingsSection();

        EditorGUILayout.EndScrollView();

        // 변경사항 적용 (Undo 지원)
        if (_serialized.ApplyModifiedProperties())
            EditorUtility.SetDirty(_manager);
    }

    // ─── 툴바 ───────────────────────────────────────
    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("VFX Master Tool", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(64)))
                TryFindManager();

            if (GUILayout.Button("전체 프리뷰 중지", EditorStyles.toolbarButton, GUILayout.Width(100)))
                ClearAllPreviews();
        }
    }

    // ─── 프리뷰 옵션 섹션 ───────────────────────────
    private void DrawPreviewOptions()
    {
        EditorGUILayout.Space(4);
        _showPreviewOptions = EditorGUILayout.Foldout(
            _showPreviewOptions, "  프리뷰 설정", true, EditorStyles.foldoutHeader);

        if (!_showPreviewOptions) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // ── 스폰 위치 ──────────────────────────
            GUILayout.Label("스폰 위치", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // SceneView 카메라 앞 옵션 켜면 XYZ 슬라이더 비활성화
                using (new EditorGUI.DisabledScope(_lockToSceneView))
                    _previewPosition = EditorGUILayout.Vector3Field("XYZ", _previewPosition);

                if (GUILayout.Button("리셋", GUILayout.Width(44)))
                    _previewPosition = Vector3.zero;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _lockToSceneView = EditorGUILayout.ToggleLeft(
                    "SceneView 카메라 앞에 스폰", _lockToSceneView);
            }

            if (_lockToSceneView)
                EditorGUILayout.HelpBox(
                    "SceneView 카메라 기준 2m 앞에 이펙트가 스폰됩니다.",
                    MessageType.Info);

            EditorGUILayout.Space(6);

            // ── 색상 오버라이드 ────────────────────
            GUILayout.Label("파티클 색상 오버라이드", EditorStyles.miniBoldLabel);

            _useColorOverride = EditorGUILayout.ToggleLeft(
                "색상 오버라이드 활성화", _useColorOverride);

            if (_useColorOverride)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _overrideColor = EditorGUILayout.ColorField("적용 색상", _overrideColor);

                    // 빠른 프리셋
                    if (GUILayout.Button("빨강", GUILayout.Width(44))) _overrideColor = new Color(1f, 0.2f, 0.1f, 1f);
                    if (GUILayout.Button("파랑", GUILayout.Width(44))) _overrideColor = new Color(0.2f, 0.5f, 1f, 1f);
                    if (GUILayout.Button("보라", GUILayout.Width(44))) _overrideColor = new Color(0.7f, 0.2f, 1f, 1f);
                    if (GUILayout.Button("황금", GUILayout.Width(44))) _overrideColor = new Color(1f, 0.8f, 0.1f, 1f);
                    if (GUILayout.Button("초록", GUILayout.Width(44))) _overrideColor = new Color(0.1f, 1f, 0.3f, 1f);
                }

                EditorGUILayout.HelpBox(
                    "Start Color만 오버라이드합니다.\n" +
                    "텍스처 기반 색상은 셰이더에 따라 적용 안 될 수 있습니다.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6);

            // ── 크기 배율 ─────────────────────────
            GUILayout.Label("파티클 크기 배율", EditorStyles.miniBoldLabel);
            _useSizeOverride = EditorGUILayout.ToggleLeft("크기 오버라이드 활성화", _useSizeOverride);

            if (_useSizeOverride)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _sizeMultiplier = EditorGUILayout.Slider("배율", _sizeMultiplier, 0.1f, 5f);
                    if (GUILayout.Button("x0.5", GUILayout.Width(40))) _sizeMultiplier = 0.5f;
                    if (GUILayout.Button("x1", GUILayout.Width(34))) _sizeMultiplier = 1f;
                    if (GUILayout.Button("x2", GUILayout.Width(34))) _sizeMultiplier = 2f;
                    if (GUILayout.Button("x3", GUILayout.Width(34))) _sizeMultiplier = 3f;
                }
            }

            EditorGUILayout.Space(6);

            // ── 수명 곡선 프리셋 ──────────────────
            GUILayout.Label("파티클 크기 곡선 프리셋", EditorStyles.miniBoldLabel);
            _useCurveOverride = EditorGUILayout.ToggleLeft("곡선 오버라이드 활성화", _useCurveOverride);

            if (_useCurveOverride)
            {
                _selectedCurve = (CurvePreset)EditorGUILayout.EnumPopup("곡선 종류", _selectedCurve);

                string desc = _selectedCurve switch
                {
                    CurvePreset.Constant => "처음부터 끝까지 크기 일정. 연기·불꽃 지속 이펙트에 적합.",
                    CurvePreset.PopBurst => "순간 최대→빠르게 소멸. 히트 스파크·타격 임팩트에 적합.",
                    CurvePreset.LinearGrow => "0에서 최대로 선형 증가. 차징·소환 이펙트에 적합.",
                    CurvePreset.Bell => "0→최대→0 종형 곡선. 폭발·버프 구체에 적합.",
                    _ => ""
                };
                EditorGUILayout.HelpBox(desc, MessageType.None);
            }

            EditorGUILayout.Space(2);
        }
    }

    // ─── 누락 프리팹 경고 ───────────────────────────
    private void DrawMissingWarnings()
    {
        var missing = new List<string>();
        foreach (var (_, fields) in Categories)
            foreach (var fieldName in fields)
            {
                var fi = GetField(fieldName);
                var val = fi?.GetValue(_manager) as GameObject;
                if (val == null) missing.Add(fieldName);
            }

        if (missing.Count == 0) return;

        _showMissing = EditorGUILayout.Foldout(_showMissing,
            $"  누락된 프리팹 {missing.Count}개", true, EditorStyles.foldoutHeader);

        if (_showMissing)
        {
            EditorGUI.indentLevel++;
            foreach (var name in missing)
                EditorGUILayout.HelpBox(name + " 가 비어 있습니다.", MessageType.Warning);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
    }

    // ─── 카테고리 섹션 ──────────────────────────────
    private void DrawCategories()
    {
        foreach (var (label, fields) in Categories)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 카테고리 헤더 + 전체 프리뷰 버튼
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(label, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("전체 재생", GUILayout.Width(BUTTON_W)))
                        PlayCategory(fields);
                }

                EditorGUILayout.Space(2);

                foreach (var fieldName in fields)
                    DrawEffectRow(fieldName);
            }
        }
    }

    // ─── 개별 이펙트 행 ─────────────────────────────
    private void DrawEffectRow(string fieldName)
    {
        var fi = GetField(fieldName);
        if (fi == null)
        {
            EditorGUILayout.HelpBox($"[코드 오류] '{fieldName}' 필드를 찾지 못했습니다.", MessageType.Error);
            return;
        }

        var prefab = fi.GetValue(_manager) as GameObject;

        using (new EditorGUILayout.HorizontalScope())
        {
            // SerializedProperty로 ObjectField 그리기 → Undo/Redo + 드래그 드롭 지원
            var prop = _serialized.FindProperty(fieldName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(NiceFieldName(fieldName)));
            }
            else
            {
                // fallback: 리플렉션 직접 표시 (SerializeField 접근 안 될 경우)
                var result = EditorGUILayout.ObjectField(
                    NiceFieldName(fieldName), prefab, typeof(GameObject), false);
                if (result != prefab) fi.SetValue(_manager, result);
            }

            // 프리뷰 버튼 — 프리팹이 없으면 비활성화
            using (new EditorGUI.DisabledScope(prefab == null))
            {
                if (GUILayout.Button("▶ 재생", GUILayout.Width(BUTTON_W)))
                    PlayPreview(prefab);
            }
        }
    }

    // ─── 설정 섹션 ──────────────────────────────────
    private void DrawSettingsSection()
    {
        EditorGUILayout.Space(6);
        _showSettings = EditorGUILayout.Foldout(_showSettings, "  파라미터 설정", true, EditorStyles.foldoutHeader);

        if (!_showSettings) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawSerializedProp("poolSizePerPrefab", "풀 사이즈 (프리팹당)");
            EditorGUILayout.Space(4);

            GUILayout.Label("피격 플래시", EditorStyles.miniBoldLabel);
            DrawSerializedProp("playerHitColor", "플레이어 피격 색상");
            DrawSerializedProp("monsterHitColor", "몬스터 피격 색상");
            DrawSerializedProp("hitFlashDuration", "플래시 지속 시간");
            EditorGUILayout.Space(4);

            GUILayout.Label("사망 디졸브", EditorStyles.miniBoldLabel);
            DrawSerializedProp("dissolveDuration", "디졸브 시간");
        }
    }

    private void DrawSerializedProp(string fieldName, string displayName)
    {
        var prop = _serialized.FindProperty(fieldName);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(displayName));
        else
            EditorGUILayout.HelpBox($"'{fieldName}' 프로퍼티를 찾지 못했습니다.", MessageType.Warning);
    }

    // ─── 프리뷰 ─────────────────────────────────────
    private void PlayPreview(GameObject prefab)
    {
        if (prefab == null) return;

        // 스폰 위치 결정
        Vector3 spawnPos = _lockToSceneView
            ? GetSceneViewSpawnPos()
            : _previewPosition;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = spawnPos;
        instance.name = $"[Preview] {prefab.name}";

        var systems = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            // 색상 오버라이드 적용
            if (_useColorOverride)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(_overrideColor);
            }
            ps.Play();
        }

        _previewInstances.Add(instance);

        // PREVIEW_SEC 후 자동 정리 예약
        _nextCleanupTime = EditorApplication.timeSinceStartup + PREVIEW_SEC;
        SceneView.RepaintAll();
    }

    /// <summary>현재 SceneView 카메라 기준 2m 앞 위치 반환</summary>
    private static Vector3 GetSceneViewSpawnPos()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return Vector3.zero;
        return sv.camera.transform.position + sv.camera.transform.forward * 2f;
    }

    private void PlayCategory(string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            var fi = GetField(name);
            var prefab = fi?.GetValue(_manager) as GameObject;
            if (prefab != null) PlayPreview(prefab);
        }
    }

    private void ClearAllPreviews()
    {
        foreach (var go in _previewInstances)
            if (go != null) DestroyImmediate(go);
        _previewInstances.Clear();
        SceneView.RepaintAll();
    }

    // ─── 헬퍼 ───────────────────────────────────────

    /// <summary>VFXManager의 private 필드를 리플렉션으로 가져옴</summary>
    private static FieldInfo GetField(string fieldName) =>
        typeof(VFXManager).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>camelCase 필드명 → 읽기 쉬운 표시 이름 변환 (예: weaponSwingPrefab → Weapon Swing)</summary>
    private static string NiceFieldName(string raw)
    {
        // "Prefab" 접미사 제거 후 공백 삽입
        var name = raw.Replace("Prefab", "");
        var result = System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");
        return char.ToUpper(result[0]) + result.Substring(1);
    }
}
