using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Material Shader Swapper v4
/// Tools > Material Shader Swapper
/// </summary>
public class MaterialShaderSwapper : EditorWindow
{
    // ─────────────────────────────────────────
    //  데이터
    // ─────────────────────────────────────────
    private class Entry
    {
        public Renderer renderer;
        public int slot;
        public Material originalMat;   // 원본 (복원용)
        public bool isSwapped;
        public bool selected = true;

        public string Label =>
            renderer != null
                ? $"{renderer.gameObject.name}  [{slot}]  {originalMat?.name ?? "null"}"
                : "(삭제됨)";

        public Material CurrentMat =>
            renderer != null && slot < renderer.sharedMaterials.Length
                ? renderer.sharedMaterials[slot]
                : null;
    }

    // ─────────────────────────────────────────
    //  상태
    // ─────────────────────────────────────────
    private Material _targetMat;
    private List<Entry> _entries = new();
    private Vector2 _scroll;
    private string _filter = "";

    // ─────────────────────────────────────────
    //  메뉴
    // ─────────────────────────────────────────
    [MenuItem("Tools/Material Shader Swapper")]
    public static void Open() =>
        GetWindow<MaterialShaderSwapper>("Shader Swapper").Show();

    // ─────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────
    private void OnGUI()
    {
        // ── 대상 머테리얼 ──
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🎨 Material Swapper",
            new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("교체할 머테리얼 (내가 만든 것)", EditorStyles.boldLabel);
        _targetMat = (Material)EditorGUILayout.ObjectField(_targetMat, typeof(Material), false);
        if (_targetMat != null)
            EditorGUILayout.LabelField($"  → {_targetMat.name}  ({_targetMat.shader?.name})",
                EditorStyles.miniLabel);
        else
            EditorGUILayout.HelpBox("여기에 머테리얼을 드래그하세요.", MessageType.Warning);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ── 스캔 버튼 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍  씬 전체 스캔", GUILayout.Height(30)))
            Scan();

        GUI.enabled = _entries.Count > 0;
        if (GUILayout.Button("전체 선택", GUILayout.Width(70))) SetAll(true);
        if (GUILayout.Button("전체 해제", GUILayout.Width(70))) SetAll(false);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // ── 검색 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("검색:", GUILayout.Width(36));
        _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22))) _filter = "";
        EditorGUILayout.EndHorizontal();

        if (_entries.Count == 0)
        {
            EditorGUILayout.HelpBox("씬 스캔을 먼저 하세요.", MessageType.None);
            return;
        }

        // ── 목록 헤더 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("", GUILayout.Width(18));
        GUILayout.Label("오브젝트 / 슬롯 / 원본 머테리얼", GUILayout.Width(260));
        GUILayout.Label("현재 머테리얼", GUILayout.MinWidth(160));
        GUILayout.Label("", GUILayout.Width(54));
        EditorGUILayout.EndHorizontal();

        // ── 목록 ──
        var list = Filtered();
        _scroll = EditorGUILayout.BeginScrollView(_scroll,
            GUILayout.Height(Mathf.Min(list.Count * 26 + 4, 380)));

        foreach (var e in list)
        {
            bool swapped = e.isSwapped;
            var bg = swapped
                ? new Color(0.20f, 0.45f, 0.20f, 0.30f)
                : new Color(0.15f, 0.15f, 0.15f, 0.15f);

            var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));
            EditorGUI.DrawRect(rowRect, bg);

            // 체크
            e.selected = EditorGUILayout.Toggle(e.selected, GUILayout.Width(18));

            // 오브젝트 이름 (클릭 → 선택)
            if (GUILayout.Button(new GUIContent(e.Label, "클릭 → Hierarchy 선택"),
                EditorStyles.miniLabel, GUILayout.Width(260)))
                if (e.renderer) Selection.activeGameObject = e.renderer.gameObject;

            // 현재 머테리얼
            var cur = e.CurrentMat;
            GUI.enabled = false;
            EditorGUILayout.ObjectField(cur, typeof(Material), false, GUILayout.MinWidth(160));
            GUI.enabled = true;

            // 교체 / 복원
            if (!swapped)
            {
                GUI.enabled = _targetMat != null;
                if (GUILayout.Button("교체", GUILayout.Width(50)))
                    DoSwap(e);
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("복원", GUILayout.Width(50)))
                    DoRestore(e);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // ── 카운트 ──
        int sc = _entries.Count(x => x.isSwapped);
        EditorGUILayout.LabelField(
            $"총 {_entries.Count}개 슬롯  |  교체됨: {sc}  |  표시: {list.Count}",
            EditorStyles.centeredGreyMiniLabel);

        // ── 하단 일괄 버튼 ──
        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = _targetMat != null && _entries.Any(e => e.selected && !e.isSwapped);
        if (GUILayout.Button("✅  선택 항목 전부 교체", GUILayout.Height(32)))
            SwapSelected();

        GUI.enabled = _entries.Any(e => e.selected && e.isSwapped);
        if (GUILayout.Button("↩  선택 항목 전부 복원", GUILayout.Height(32)))
            RestoreSelected();

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUI.color = new Color(1f, 0.45f, 0.45f);
        if (GUILayout.Button("⚠  전체 복원", GUILayout.Height(24)))
            if (EditorUtility.DisplayDialog("전체 복원", "모두 원본으로 되돌립니까?", "복원", "취소"))
                RestoreAll();
        GUI.color = Color.white;
    }

    // ─────────────────────────────────────────
    //  스캔
    // ─────────────────────────────────────────
    private void Scan()
    {
        RestoreAll();          // 이전 교체 먼저 복원
        _entries.Clear();

        foreach (var r in FindObjectsOfType<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                _entries.Add(new Entry
                {
                    renderer = r,
                    slot = i,
                    originalMat = mats[i],
                    isSwapped = false
                });
            }
        }
        Debug.Log($"[Swapper] 스캔 완료: {_entries.Count}개 슬롯");
        Repaint();
    }

    // ─────────────────────────────────────────
    //  교체 — 3단계 폴백
    // ─────────────────────────────────────────
    private void DoSwap(Entry e)
    {
        if (_targetMat == null || e.renderer == null) return;

        Undo.RecordObject(e.renderer, "Material Swap");

        bool ok = false;

        // ① SerializedObject (FBX/프리팹 잠금 우회)
        ok = TrySetMaterialSerialized(e.renderer, e.slot, _targetMat);

        // ② 일반 sharedMaterials 배열 할당
        if (!ok)
        {
            var arr = e.renderer.sharedMaterials;
            if (e.slot < arr.Length)
            {
                arr[e.slot] = _targetMat;
                e.renderer.sharedMaterials = arr;
                ok = true;
            }
        }

        // ③ 마지막 수단: materials (인스턴스) 배열
        if (!ok)
        {
            var arr = e.renderer.materials;
            if (e.slot < arr.Length)
            {
                arr[e.slot] = _targetMat;
                e.renderer.materials = arr;
                ok = true;
            }
        }

        if (ok)
        {
            e.isSwapped = true;
            // 실제 적용됐는지 확인 로그
            var cur = e.renderer.sharedMaterials;
            string curName = e.slot < cur.Length ? cur[e.slot]?.name ?? "null" : "OOB";
            Debug.Log($"[Swapper] 교체: {e.renderer.gameObject.name}[{e.slot}]  " +
                      $"→ {_targetMat.name}  (실제 적용: {curName})");
        }
        else
        {
            Debug.LogWarning($"[Swapper] 교체 실패: {e.renderer.gameObject.name}[{e.slot}]");
        }

        EditorUtility.SetDirty(e.renderer.gameObject);
        Repaint();
    }

    // ─────────────────────────────────────────
    //  복원
    // ─────────────────────────────────────────
    private void DoRestore(Entry e)
    {
        if (!e.isSwapped || e.renderer == null) return;

        Undo.RecordObject(e.renderer, "Material Restore");

        bool ok = TrySetMaterialSerialized(e.renderer, e.slot, e.originalMat);
        if (!ok)
        {
            var arr = e.renderer.sharedMaterials;
            if (e.slot < arr.Length)
            {
                arr[e.slot] = e.originalMat;
                e.renderer.sharedMaterials = arr;
            }
        }

        e.isSwapped = false;
        EditorUtility.SetDirty(e.renderer.gameObject);
        Repaint();
    }

    // ─────────────────────────────────────────
    //  SerializedObject 방식
    // ─────────────────────────────────────────
    private static bool TrySetMaterialSerialized(Renderer r, int slot, Material mat)
    {
        try
        {
            var so = new SerializedObject(r);
            var prop = so.FindProperty("m_Materials");
            if (prop == null || !prop.isArray) return false;

            // 슬롯이 배열 범위 밖이면 확장
            while (prop.arraySize <= slot)
                prop.InsertArrayElementAtIndex(prop.arraySize);

            prop.GetArrayElementAtIndex(slot).objectReferenceValue = mat;
            so.ApplyModifiedPropertiesWithoutUndo();   // Undo는 상위에서 RecordObject로 처리
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ─────────────────────────────────────────
    //  일괄 / 전체
    // ─────────────────────────────────────────
    private void SwapSelected()
    {
        Undo.SetCurrentGroupName("Swap Materials (Batch)");
        int n = 0;
        foreach (var e in Filtered().Where(e => e.selected && !e.isSwapped))
        { DoSwap(e); n++; }
        Debug.Log($"[Swapper] 일괄 교체: {n}개");
    }

    private void RestoreSelected()
    {
        Undo.SetCurrentGroupName("Restore Materials (Batch)");
        foreach (var e in Filtered().Where(e => e.selected && e.isSwapped))
            DoRestore(e);
    }

    private void RestoreAll()
    {
        foreach (var e in _entries.Where(e => e.isSwapped).ToList())
            DoRestore(e);
    }

    // ─────────────────────────────────────────
    //  필터 / 유틸
    // ─────────────────────────────────────────
    private List<Entry> Filtered() =>
        _entries.Where(e =>
        {
            if (e.renderer == null) return false;
            if (!string.IsNullOrEmpty(_filter) &&
                !e.Label.ToLower().Contains(_filter.ToLower())) return false;
            return true;
        }).ToList();

    private void SetAll(bool v) { foreach (var e in Filtered()) e.selected = v; }
}