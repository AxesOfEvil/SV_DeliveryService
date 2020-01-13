using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryService.Framework
{
    public class BaseDataModel
    {
        public string Location;
        public int X;
        public int Y;
        public bool isFridge;
        public BaseDataModel() { }
        public BaseDataModel(string location, int x, int y, bool is_fridge)
        {
            Location = location;
            X = x;
            Y = y;
            isFridge = is_fridge;
        }
        public BaseDataModel(DeliveryChest chest)
        {
            Location = chest.Location.Name;
            X = (int)chest.TileLocation.X;
            Y = (int)chest.TileLocation.Y;
            isFridge = chest.IsFridge();
        }
    }
}
