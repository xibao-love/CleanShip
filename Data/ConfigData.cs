using System.Collections.Generic;

namespace CleanShip
{
    [System.Serializable]
    public class ItemLocationList
    {
        public float winWidth = 600f;
        public float winHeight = 800f;
        public int fontSize = 16;
        public bool onlySortCustom = false;
        public List<ItemData> items = new List<ItemData>();
    }

    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public float x;
        public float y;
        public float z;
    }
}