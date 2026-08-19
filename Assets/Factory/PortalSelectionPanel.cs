using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maptory.Factory
{
    public sealed class PortalSelectionPanel : MonoBehaviour
    {
        private static readonly Color PANEL_COLOR = new(0.14f, 0.16f, 0.23f, 1f);
        private static readonly Color ROW_COLOR = new(0.86f, 0.89f, 0.94f, 1f);
        private static readonly Color LOCKED_COLOR = new(0.22f, 0.24f, 0.29f, 1f);
        private static readonly Color ROW_TEXT_COLOR = new(0.08f, 0.1f, 0.16f, 1f);
        private static readonly Color CLOSE_COLOR = new(0.57f, 0.18f, 0.2f, 1f);
        private static readonly Color ERROR_COLOR = new(1f, 0.4f, 0.36f, 1f);

        private readonly List<GroundRow> rows = new();
        private FactoryTileCatalog catalog;
        private FactoryProgression progression;
        private FactoryStageDefinition stage;
        private PortalState portal;
        private GameObject blocker;
        private GameObject unlock_popup;
        private TMP_Text unlock_title;
        private TMP_Text unlock_monster;
        private TMP_Text unlock_meso;
        private TMP_Text unlock_material;
        private TMP_Text unlock_status;
        private Button confirm_unlock;
        private HuntingGroundDefinition pending_ground;

        public bool IsOpen => blocker != null && blocker.activeSelf;
        public int RowCount => rows.Count;

        public static PortalSelectionPanel Create(
            Transform parent,
            FactoryTileCatalog catalog,
            FactoryProgression progression = null,
            FactoryStageDefinition stage = null)
        {
            var canvas_object = new GameObject(
                "Portal Selection UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas_object.transform.SetParent(parent, false);

            var canvas = canvas_object.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            var scaler = canvas_object.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;

            var panel = canvas_object.AddComponent<PortalSelectionPanel>();
            panel.catalog = catalog;
            panel.progression = progression;
            panel.stage = stage;
            panel.Build();
            return panel;
        }

        public void Show(PortalState target)
        {
            portal = target;
            blocker.SetActive(true);
            RefreshRows();
        }

        private void Update()
        {
            if (unlock_popup != null && unlock_popup.activeSelf) RefreshUnlockPopup();
        }

        private void Build()
        {
            blocker = CreateObject("Blocker", transform);
            Stretch(blocker.GetComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            var grounds = GetGrounds();
            var panel = CreateObject("Panel", blocker.transform);
            var panel_rect = panel.GetComponent<RectTransform>();
            panel_rect.anchorMin = panel_rect.anchorMax = new Vector2(0.5f, 0.5f);
            panel_rect.pivot = new Vector2(0.5f, 0.5f);
            panel_rect.anchoredPosition = Vector2.zero;
            panel_rect.sizeDelta = new Vector2(620f, 104f + grounds.Count * 54f);
            panel.AddComponent<Image>().color = PANEL_COLOR;

            CreateText(
                stage == null ? "공급 사냥터 / 몬스터 선택" : $"{stage.DisplayName} 사냥터 선택",
                panel.transform,
                25f,
                TextAlignmentOptions.Center,
                new Vector2(90f, -18f),
                new Vector2(440f, 40f),
                Color.white);

            var close = CreateButton("Close", panel.transform, "X", 23f, CLOSE_COLOR, Color.white);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(570f, -19f), new Vector2(36f, 36f));
            close.onClick.AddListener(Close);

            for (var index = 0; index < grounds.Count; index++)
            {
                var ground = grounds[index];
                var row = CreateButton(ground.Id, panel.transform, ground.SupplyOption.SelectionLabel,
                    18f, ROW_COLOR, ROW_TEXT_COLOR);
                SetRect(row.GetComponent<RectTransform>(), new Vector2(14f, -78f - index * 54f), new Vector2(478f, 44f));
                row.onClick.AddListener(() => Select(ground));

                var unlock = CreateButton("Unlock " + ground.Id, panel.transform, "해금", 17f,
                    new Color(0.42f, 0.34f, 0.12f, 1f), Color.white);
                SetRect(unlock.GetComponent<RectTransform>(), new Vector2(502f, -78f - index * 54f), new Vector2(104f, 44f));
                unlock.onClick.AddListener(() => ShowUnlock(ground));
                rows.Add(new GroundRow(ground, row, unlock));
            }

            BuildUnlockPopup(blocker.transform);
            blocker.SetActive(false);
        }

        private IReadOnlyList<HuntingGroundDefinition> GetGrounds()
        {
            if (stage != null) return stage.HuntingGrounds;

            var all = new List<HuntingGroundDefinition>();
            foreach (var option in PortalSupplyCatalog.Options)
            {
                all.Add(new HuntingGroundDefinition(option.Material.ToString(), option.Material,
                    true, 0L, option.Material, 0L));
            }
            return all;
        }

        private void RefreshRows()
        {
            foreach (var row in rows)
            {
                var unlocked = progression == null || progression.IsHuntingGroundUnlocked(row.Ground.Id);
                row.SelectButton.interactable = unlocked;
                row.UnlockButton.gameObject.SetActive(!unlocked);
                var image = row.SelectButton.GetComponent<Image>();
                image.color = unlocked ? ROW_COLOR : LOCKED_COLOR;
                var label = row.SelectButton.GetComponentInChildren<TMP_Text>();
                label.color = unlocked ? ROW_TEXT_COLOR : Color.white;
                label.text = unlocked
                    ? row.Ground.SupplyOption.SelectionLabel
                    : $"[잠금] {row.Ground.SupplyOption.SelectionLabel}";
            }
        }

        private void Select(HuntingGroundDefinition ground)
        {
            if (progression != null && !progression.IsHuntingGroundUnlocked(ground.Id)) return;

            portal.SelectMaterial(ground.Monster);
            Close();
        }

        private void ShowUnlock(HuntingGroundDefinition ground)
        {
            pending_ground = ground;
            unlock_popup.SetActive(true);
            RefreshUnlockPopup();
        }

        private void BuildUnlockPopup(Transform parent)
        {
            unlock_popup = CreateObject("Hunting Ground Unlock Popup", parent);
            Stretch(unlock_popup.GetComponent<RectTransform>());
            unlock_popup.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.66f);

            var popup = CreateObject("Popup", unlock_popup.transform);
            var rect = popup.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 350f);
            popup.AddComponent<Image>().color = PANEL_COLOR;

            unlock_title = CreateText("", popup.transform, 28f, TextAlignmentOptions.Center,
                new Vector2(40f, -24f), new Vector2(420f, 44f), Color.white);
            unlock_monster = CreateText("", popup.transform, 20f, TextAlignmentOptions.Center,
                new Vector2(40f, -79f), new Vector2(420f, 36f), Color.white);
            unlock_meso = CreateText("", popup.transform, 19f, TextAlignmentOptions.MidlineLeft,
                new Vector2(52f, -132f), new Vector2(396f, 34f), Color.white);
            unlock_material = CreateText("", popup.transform, 19f, TextAlignmentOptions.MidlineLeft,
                new Vector2(52f, -174f), new Vector2(396f, 34f), Color.white);
            unlock_status = CreateText("", popup.transform, 17f, TextAlignmentOptions.Center,
                new Vector2(52f, -219f), new Vector2(396f, 42f), Color.white);

            var cancel = CreateButton("Cancel Unlock", popup.transform, "취소", 19f, LOCKED_COLOR, Color.white);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(50f, -282f), new Vector2(180f, 50f));
            cancel.onClick.AddListener(() => unlock_popup.SetActive(false));
            confirm_unlock = CreateButton("Confirm Unlock", popup.transform, "해금", 19f,
                new Color(0.42f, 0.34f, 0.12f, 1f), Color.white);
            SetRect(confirm_unlock.GetComponent<RectTransform>(), new Vector2(270f, -282f), new Vector2(180f, 50f));
            confirm_unlock.onClick.AddListener(ConfirmUnlock);
            unlock_popup.SetActive(false);
        }

        private void RefreshUnlockPopup()
        {
            if (pending_ground == null) return;

            var meso_ready = progression.Economy.CanSpendMeso(pending_ground.UnlockMesoCost);
            var current_material = progression.Economy.GetAvailableProduction(pending_ground.RequiredMaterial);
            var material_ready = current_material >= pending_ground.RequiredAmount;
            unlock_title.text = pending_ground.SupplyOption.SourceName + " 해금";
            unlock_monster.text = $"사용 가능 몬스터: {pending_ground.SupplyOption.ItemLabel}";
            unlock_meso.text = $"메소  {progression.Economy.TotalMeso:N0} / {pending_ground.UnlockMesoCost:N0}";
            unlock_meso.color = meso_ready ? Color.white : ERROR_COLOR;
            unlock_material.text = $"{pending_ground.RequiredMaterial.ToKoreanName()}  "
                + $"{current_material:N0} / {pending_ground.RequiredAmount:N0}";
            unlock_material.color = material_ready ? Color.white : ERROR_COLOR;

            var missing = new List<string>();
            if (!meso_ready) missing.Add("메소 부족");
            if (!material_ready) missing.Add("재료 부족");
            unlock_status.text = missing.Count == 0
                ? "조건을 만족했습니다. 구매 시 두 재화가 함께 차감됩니다."
                : string.Join(" · ", missing);
            unlock_status.color = missing.Count == 0 ? Color.white : ERROR_COLOR;
            confirm_unlock.interactable = missing.Count == 0;
        }

        private void ConfirmUnlock()
        {
            if (!progression.TryUnlockHuntingGround(pending_ground.Id)) return;

            unlock_popup.SetActive(false);
            RefreshRows();
        }

        private void Close()
        {
            unlock_popup.SetActive(false);
            blocker.SetActive(false);
        }

        private Button CreateButton(string name, Transform parent, string label, float font_size,
            Color background_color, Color text_color)
        {
            var game_object = CreateObject(name, parent);
            var image = game_object.AddComponent<Image>();
            image.color = background_color;
            var button = game_object.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(label, game_object.transform, font_size,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, text_color);
            Stretch(text.rectTransform);
            return button;
        }

        private TMP_Text CreateText(string value, Transform parent, float size,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, Color color)
        {
            var text = CreateObject("Text", parent).AddComponent<TextMeshProUGUI>();
            text.font = catalog.UiFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            SetRect(text.rectTransform, position, dimensions);
            return text;
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var game_object = new GameObject(name, typeof(RectTransform));
            game_object.transform.SetParent(parent, false);
            return game_object;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private sealed class GroundRow
        {
            public HuntingGroundDefinition Ground { get; }
            public Button SelectButton { get; }
            public Button UnlockButton { get; }

            public GroundRow(HuntingGroundDefinition ground, Button select_button, Button unlock_button)
            {
                Ground = ground;
                SelectButton = select_button;
                UnlockButton = unlock_button;
            }
        }
    }
}
