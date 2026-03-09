using System;
using System.Linq;
using UnityEngine;
using Vector2 = UnityEngine.Vector2; 
namespace CleanShip
{
    public partial class Plugin
    {
        private void OnGUI()
        {
            if (!isMenuOpen) return;

            int size = customLocations.fontSize;
            GUI.skin.label.fontSize = size;
            GUI.skin.button.fontSize = size;
            GUI.skin.textField.fontSize = size;
            GUI.skin.box.fontSize = size;
            GUI.skin.toggle.fontSize = size;
            GUI.skin.window.fontSize = size + 2;

            GUI.backgroundColor = Color.black;

            if (Mathf.Abs(winRect.width - customLocations.winWidth) > 1f) winRect.width = customLocations.winWidth;
            if (Mathf.Abs(winRect.height - customLocations.winHeight) > 1f) winRect.height = customLocations.winHeight;

            try
            {
                winRect = GUILayout.Window(9999, winRect, WindowFunction, "CleanShip 物品整理控制台");
            }
            catch
            {
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
            }
        }

        void WindowFunction(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("--- 基础操作 ---");
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = Setting.bCleaning ? Color.green : Color.white;
            if (GUILayout.Button(Setting.bCleaning ? "停止整理" : "整理飞船", GUILayout.Height(35)))
            {
                if (!Setting.bCleaning) StartCoroutine(SortCoroutine());
                else { Setting.bCleaning = false; StopAllCoroutines(); }
            }

            GUI.backgroundColor = customLocations.onlySortCustom ? Color.cyan : Color.gray;
            if (GUILayout.Button(customLocations.onlySortCustom ? "模式: 仅自定义" : "模式: 全部整理", GUILayout.Height(35)))
            {
                customLocations.onlySortCustom = !customLocations.onlySortCustom;
                SaveCustomLocations();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUI.color = Color.cyan;
            GUILayout.Label("--- 飞船内物品扫描 ---");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Width(80))) RefreshShipItems();
            GUILayout.Label("搜:", GUILayout.Width(40));
            searchKeyword = GUILayout.TextField(searchKeyword);
            GUILayout.EndHorizontal();

            shipItemsScrollPosition = GUILayout.BeginScrollView(shipItemsScrollPosition, GUI.skin.box, GUILayout.Height(180));
            var filteredList = detectedShipItemNames.Where(n => n != null && n.ToLower().Contains(searchKeyword.ToLower())).ToList();

            if (filteredList.Count == 0) GUILayout.Label("无匹配物品");
            else
            {
                foreach (string itemName in filteredList)
                {
                    GUILayout.BeginHorizontal();
                    bool isConfigured = customLocations.items.Any(x => x != null && x.itemName == itemName);
                    GUI.color = isConfigured ? Color.green : Color.white;
                    GUILayout.Label(itemName);
                    GUI.color = Color.white;
                    if (GUILayout.Button("设为脚下", GUILayout.Width(100))) RecordItemByName(itemName);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(15);
            GUI.color = Color.yellow;
            GUILayout.Label($"--- 已保存配置 ({customLocations.items.Count}) ---");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("记录手持")) RecordCurrentItem();
            if (GUILayout.Button("保存配置")) { SaveCustomLocations(); Log.LogInfo("配置已保存！"); }
            GUILayout.EndHorizontal();

            configScrollPosition = GUILayout.BeginScrollView(configScrollPosition, GUI.skin.box, GUILayout.Height(200));
            for (int i = 0; i < customLocations.items.Count; i++)
            {
                var itemData = customLocations.items[i];
                if (itemData == null) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(itemData.itemName, GUILayout.Width(180));

                itemData.x = float.TryParse(GUILayout.TextField(itemData.x.ToString("F1"), GUILayout.Width(50)), out float nx) ? nx : itemData.x;
                itemData.y = float.TryParse(GUILayout.TextField(itemData.y.ToString("F1"), GUILayout.Width(50)), out float ny) ? ny : itemData.y;
                itemData.z = float.TryParse(GUILayout.TextField(itemData.z.ToString("F1"), GUILayout.Width(50)), out float nz) ? nz : itemData.z;

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    customLocations.items.RemoveAt(i);
                    SaveCustomLocations();
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(15);
            GUI.color = Color.magenta;
            GUILayout.Label("--- 界面设置 ---");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"字体: {customLocations.fontSize}", GUILayout.Width(80));
            int newSize = (int)GUILayout.HorizontalSlider(customLocations.fontSize, 10f, 30f);
            if (newSize != customLocations.fontSize) customLocations.fontSize = newSize;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"宽: {(int)customLocations.winWidth}", GUILayout.Width(80));
            float newW = GUILayout.HorizontalSlider(customLocations.winWidth, 400f, 1000f);
            if (Mathf.Abs(newW - customLocations.winWidth) > 1f) customLocations.winWidth = newW;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"高: {(int)customLocations.winHeight}", GUILayout.Width(80));
            float newH = GUILayout.HorizontalSlider(customLocations.winHeight, 600f, 1300f);
            if (Mathf.Abs(newH - customLocations.winHeight) > 1f) customLocations.winHeight = newH;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("保存界面设置")) SaveCustomLocations();

            GUILayout.EndVertical();

            // 拖动区域
            GUI.DragWindow(new Rect(0, 0, customLocations.winWidth, 30));
        }
    }
}