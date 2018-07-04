using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XNAHuntTheWumpus
{
    class Player
    {
        public int pos=-1;
        public int arrows = 5; //number of arrows player has
        public Player(int pos)
        {
            this.pos = pos;
        }
    }

}
