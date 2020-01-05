using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;


namespace DeliveryService
{
    public class ItemDescriptor
    {
        private string Type;
        private string Name;
        public static implicit operator string(ItemDescriptor d)
        {
            //return d.Type + "/" + d.Name;
            return d.Name;
        }
        public ItemDescriptor(string id)
        {
            char[] sep = { '/' };
            string[] split = id.Split(sep, 2);
            if (split.Length != 2)
            {
                this.Type = "Object";
                this.Name = id;
            } else
            {
                this.Type = split[0];
                this.Name = split[1];
            }
        }
        public ItemDescriptor(string type, string name)
        {
            this.Type = type;
            this.Name = name;
        }
        public bool MatchItem(SObject item)
        {
            //return (item.Type == this.Type && item.Name == this.Name);
            return (item.Name == this.Name);
        }
    }
    public class ItemPair
    {
        public  string FromID;
        public  string ToID;
        // private ItemDescriptor _From = null;
        // private ItemDescriptor _To = null;
        public ItemPair(string from_id, string to_id)
        {
            // this._From = new ItemDescriptor(from_id);
            // this._To = new ItemDescriptor(to_id);
            // this.FromID = this._From;
            // this.ToID = this._To;
            this.FromID = from_id;
            this.ToID = to_id;
        }
        public ItemDescriptor GetFromId() {
            //if (this._From is null) {
            //    this._From = new ItemDescriptor(this.FromID);
            //}
            //return this._From;
            return new ItemDescriptor(this.FromID);
        }
        public ItemDescriptor GetToId()
        {
            //if (this._To is null)
            //{
            //    this._To = new ItemDescriptor(this.ToID);
            //}
            //return this._To;
            return new ItemDescriptor(this.ToID);
        }
    }
    class ModConfig
    {
        public List<ItemPair> DeliveryMap { get; set; }
        public ModConfig()
        {
            this.DeliveryMap = new List<ItemPair>();
            this.DeliveryMap.Add(new ItemPair("Auto-Grabber", "Stardew Hero Trophy"));
        }
    }
}
