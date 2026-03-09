using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using Vector2 = UnityEngine.Vector2; 

namespace CleanShip
{
    public partial class Plugin
    {
        // --- 反射工具 ---
        private T GetPrivateField<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) return (T)field.GetValue(instance);
            return default(T);
        }

        private void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) field.SetValue(instance, value);
        }

        private void CallPrivateMethod(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null) method.Invoke(instance, args);
        }

        private IEnumerator SortCoroutine()
        {
            Setting.bCleaning = true;
            PlayerControllerB player = GameNetworkManager.Instance?.localPlayerController;
            Transform shipTransform = StartOfRound.Instance?.elevatorTransform;

            if (shipTransform == null || player == null) { Setting.bCleaning = false; yield break; }

            GrabbableObject[] items = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            foreach (var item in items)
            {
                if (!Setting.bCleaning || player.isPlayerDead || !player.isPlayerControlled || StartOfRound.Instance.inShipPhase == false)
                {
                    break;
                }

                if (item == null || item.itemProperties == null) continue;
                bool isPhysicallyInShip = Vector3.Distance(item.transform.position, shipTransform.position) < 18f;
                if (item.isHeld || item.heldByPlayerOnServer) continue;
                if (!item.isInShipRoom && !isPhysicallyInShip) continue;

                ItemData customData = customLocations.items.FirstOrDefault(x => x != null && x.itemName == item.itemProperties.itemName);
                if (customLocations.onlySortCustom && customData == null) continue;

                Vector3 targetPos = new Vector3(-3.3f, 0.5f, -12.7f);
                if (customData != null) targetPos = new Vector3(customData.x, customData.y, customData.z);

                if (Vector3.Distance(item.transform.position, targetPos) > 0.2f)
                {
                    yield return new WaitUntil(() => player.isPlayerDead || !GetPrivateField<bool>(player, "isThrowingObject"));
                    if (player.isPlayerDead) break;

                    NetworkObjectReference netObjRef = new NetworkObjectReference(item.NetworkObject);
                    SetPrivateField(player, "currentlyGrabbingObject", item);
                    CallPrivateMethod(player, "GrabObjectServerRpc", netObjRef);

                    float waitTimer = 0f;
                    yield return new WaitUntil(() => {
                        waitTimer += Time.deltaTime;
                        return player.isPlayerDead || player.currentlyHeldObjectServer == item || waitTimer > 1.0f;
                    });

                    if (player.isPlayerDead || player.currentlyHeldObjectServer != item) continue;

                    player.DiscardHeldObject(true, null, targetPos, false);
                    item.transform.rotation = Quaternion.Euler(0, 0, 0);
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    if (Quaternion.Angle(item.transform.rotation, Quaternion.Euler(0, 0, 0)) > 5f) item.transform.rotation = Quaternion.Euler(0, 0, 0);
                }
            }
            Setting.bCleaning = false;
        }

        private void RefreshShipItems()
        {
            detectedShipItemNames.Clear();
            if (StartOfRound.Instance == null || StartOfRound.Instance.elevatorTransform == null) return;
            Transform shipTransform = StartOfRound.Instance.elevatorTransform;
            GrabbableObject[] allItems = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            foreach (var item in allItems)
            {
                if (item.isHeld || item.heldByPlayerOnServer) continue;
                if (Vector3.Distance(item.transform.position, shipTransform.position) > 20f) continue;
                string name = item.itemProperties?.itemName;
                if (name != null && !detectedShipItemNames.Contains(name)) detectedShipItemNames.Add(name);
            }
            detectedShipItemNames.Sort();
        }

        private void RecordItemByName(string itemName)
        {
            var player = GameNetworkManager.Instance?.localPlayerController;
            if (player == null) return;
            Vector3 pos = player.transform.position;
            ItemData existing = customLocations.items.FirstOrDefault(x => x != null && x.itemName == itemName);
            if (existing != null)
            {
                existing.x = pos.x;
                existing.y = pos.y;
                existing.z = pos.z;
            }
            else
            {
                customLocations.items.Add(new ItemData { itemName = itemName, x = pos.x, y = pos.y, z = pos.z });
            }
            SaveCustomLocations();
        }

        private void RecordCurrentItem()
        {
            var player = GameNetworkManager.Instance?.localPlayerController;
            if (player == null || player.currentlyHeldObjectServer == null || player.currentlyHeldObjectServer.itemProperties == null) return;
            RecordItemByName(player.currentlyHeldObjectServer.itemProperties.itemName);
        }
    }
}