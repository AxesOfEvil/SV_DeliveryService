using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using SObject = StardewValley.Object;

namespace DeliveryService.Framework
{
    enum DeliveryCategories
    {
        Animal_Product,
        Artifact,
        Artisan_Goods,
        Cooking,
        Crafting,
        Fish,
        Flower,
        Forage,
        Fruit,
        Mineral,
        Monster_Loot,
        Resource,
        Seed,
        Vegetable,
        Tool,
        Trash,
        Misc
    }
    static class DeliveryCategoriesMethods
    {
        public static string Name(this DeliveryCategories category)
        {
            return category.ToString().Replace("_", " ");
        }
    }
    static class ItemMethods
    {
        public static DeliveryCategories getDeliveryCategory(this Item item)
        {
            SObject obj = null;
            if (item is SObject sobj)
            {
                obj = sobj;
            }
            foreach (DeliveryCategories cat in Enum.GetValues(typeof(DeliveryCategories)))
            {
                switch (cat)
                {
                    case DeliveryCategories.Animal_Product:
                    case DeliveryCategories.Artifact:
                    case DeliveryCategories.Artisan_Goods:
                    case DeliveryCategories.Cooking:
                    case DeliveryCategories.Fish:
                    case DeliveryCategories.Flower:
                    case DeliveryCategories.Forage:
                    case DeliveryCategories.Fruit:
                    case DeliveryCategories.Mineral:
                    case DeliveryCategories.Monster_Loot:
                    case DeliveryCategories.Resource:
                    case DeliveryCategories.Seed:
                    case DeliveryCategories.Vegetable:
                    case DeliveryCategories.Tool:
                    case DeliveryCategories.Trash:
                        if (item.getCategoryName() == cat.Name())
                            return cat;
                        break;
                    case DeliveryCategories.Crafting:
                        if (obj != null && obj.Type == cat.Name())
                            return cat;
                        break;
                }
            }
            return DeliveryCategories.Misc;
        }
    }
}
