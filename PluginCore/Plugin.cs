using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

namespace CleanShip
{
    [BepInPlugin("me.cleanship.mod", "CleanShip", "1.0.0")]
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

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo(">>> CleanShip 物品整理模组加载中... <<<");

            configPath = Path.Combine(Paths.ConfigPath, "CleanShip_Items.json");
            LoadCustomLocations();
            winRect = new Rect(50, 50, customLocations.winWidth, customLocations.winHeight);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.equalsKey.wasPressedThisFrame)
            {
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