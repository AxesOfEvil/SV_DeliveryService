using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Objects;

namespace DeliveryService.Framework
{
    internal class DeliveryChest
    {
        /// <summary>The location or building which contains the chest.</summary>
        public DeliverySign Sign { get; }

        /// <summary>The chest's tile position within its location or building.</summary>
        public Chest Chest { get; }

        public DeliveryChest(DeliverySign sign, Chest chest)
        {
            this.Sign = sign;
            this.Chest = chest;
        }

        public GameLocation Location()
        {
            return this.Sign.Location;
        }
    }
}