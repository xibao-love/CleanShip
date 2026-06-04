using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanShip
{
    // ========== 新增：生命周期代理类 ==========
    public class CleanShipRunner : MonoBehaviour
    {
        public static CleanShipRunner Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnGUI()
        {
            Plugin.Instance?.OnGUI();
        }
    }
    // =========================================

    [BepInPlugin("me.cleanship.mod", "CleanShip", "1.0.2")]
    public partial class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static Plugin Instance;

        // --- 窗口与配置变量 ---
        private Rect winRect;
        private bool isMenuOpen = false;
        private Vector2 configScrollPosition = Vector2.zero;
        private Vector2 shipItemsScrollPosition = Vector2.zero;
        private string searchKeyword = "";
        private string configPath;

        // --- 运行时数据 ---
        public ItemLocationList customLocations = new ItemLocationList();
        private List<string> detectedShipItemNames = new List<string>();

        // --- 按键绑定 (取代 Update) ---
        private InputAction menuKeyAction;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo(">>> CleanShip 物品整理模组加载中... <<<");

            // ========== 新增：创建生命周期代理 GameObject ==========
            GameObject runnerGo = new GameObject("CleanShip_LifecycleManager");
            DontDestroyOnLoad(runnerGo);
            runnerGo.hideFlags = HideFlags.HideAndDontSave;
            runnerGo.AddComponent<CleanShipRunner>();
            // ====================================================

            configPath = Path.Combine(Paths.ConfigPath, "CleanShip_Items.json");
            LoadCustomLocations();
            winRect = new Rect(50, 50, customLocations.winWidth, customLocations.winHeight);

            menuKeyAction = new InputAction("OpenCleanShipMenu", binding: "<Keyboard>/equals");
            menuKeyAction.performed += ToggleMenu;
            menuKeyAction.Enable();
        }

        private void ToggleMenu(InputAction.CallbackContext context)
        {
            Log.LogInfo("ToggleMenu 被调用！");   // 新增
            isMenuOpen = !isMenuOpen;
            Setting.bMenu = isMenuOpen;

            if (isMenuOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                RefreshShipItems();
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void SaveCustomLocations()
        {
            string json = JsonUtility.ToJson(customLocations, true);
            File.WriteAllText(configPath, json);
        }

        private void LoadCustomLocations()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    customLocations = JsonUtility.FromJson<ItemLocationList>(json);
                }
                catch { customLocations = new ItemLocationList(); }
            }
            else
            {
                customLocations = new ItemLocationList();
            }
        }
    }
}