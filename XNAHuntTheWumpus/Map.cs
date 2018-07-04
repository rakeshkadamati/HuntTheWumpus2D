using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XNAHuntTheWumpus;

namespace XNAHuntTheWumpus
{
    class Map
    {
        private const int NUMBATS = 2;
        private const int NUMPITS = 2;
        private int seed;
       
        public int[][] Rooms;
        public int[] Bats; //locations of bats
        public int[] Pits; //locations of pits

        public Player player;
        public Wumpus wump;

        public Random randomGenerator;

        //Creates a new map for the game
        public Map()
        {
            seed = DateTime.Now.Millisecond;
            SetupMap();
        }

        private void SetupMap()
        {

            randomGenerator = new Random(seed);
            InitRooms();

            //generate random ints for player starting pos, superbats, pits, and wumpus
            List<int> RandomRooms = new List<int>();

            while (RandomRooms.Count < NUMBATS + NUMPITS + 2)
            {
                int rand = randomGenerator.Next(1, 20);
                if (!RandomRooms.Contains(rand))
                    RandomRooms.Add(rand);
            }
            player = new Player(RandomRooms[0]);
            wump = new Wumpus(RandomRooms[1]);
            Bats = new int[NUMBATS] { RandomRooms[2], RandomRooms[3] };
            Pits = new int[NUMPITS] { RandomRooms[4], RandomRooms[5] };
        }

        public void PlayerMove(int room, ref Game1.State currentGameState)
        {
            //update player pos to room
            player.pos = room;
            if (Pits.Contains(room))
                currentGameState = Game1.State.Fell; //update state
            else if (Bats.Contains(room))
            {
                currentGameState = Game1.State.Superbat; //update state
                //generate new room for player to be carried to
                int newRoom = -1;
                do
                {
                    newRoom = randomGenerator.Next(1, 20);
                } while (newRoom == room);
                player.pos = newRoom;
            }
            else if (wump.pos == room) //if u ran into the wumpus!
            {
                //check if already awake
                if (wump.awake)
                {
                    currentGameState = Game1.State.WumpWin;
                    ReplayGame();
                }
                else //wump was sleeping
                {
                    wump.awake = true; //wumpus used awaken!
                    WumpusMove();
                    //EndTurn();
                }
            }

        }

        //private bool EndTurn()
        //{

        //    if (wump.pos == player.pos)
        //    {

        //        ReplayGame();
        //        return true;
        //    }
        //    return false;
        //}

        public void CheckIfWumpusAwake()
        {
            if (wump.awake)
                WumpusMove();
        }

        public void WumpusMove()
        {
            if (wump.awake)
            {
                int rand = randomGenerator.Next(1, 100);
                //if rand is <25 then Wumpus stays, eklse it moves to adjacent room
                if (rand <= 25)//stay-do nothing
                {
                }
                else
                {
                    wump.pos = Rooms[wump.pos][randomGenerator.Next(0, 2)]; //move to random adjacent room
                }

            }
        }

        public void NewGame()
        {
            seed = DateTime.Now.Millisecond;
            SetupMap();
        }
        public void ReplayGame()
        {
            SetupMap();
        }

        public void ShootArrow(ref int[] rooms, int numRooms, ref Game1.State currentGameState)
        {
            if (player.arrows > 0)
            {
                bool wumpAwoken = !wump.awake; //if wumpus was awaken this turn to ensure we perform WumpusMove before EndTurn
                wump.awake = true; //awaken wumpus
                player.arrows--;
                int currentArrowLocation = player.pos;
                int[] arrowTravellingRooms = new int[6]; //keeps track of rooms arrow will actualy travel through
                arrowTravellingRooms[0] = currentArrowLocation;
                int count = 0;

                Console.WriteLine("Arrow went through rooms: ");
                for (int i =0; i<numRooms; i++)
                {
                    int arrowRoom = rooms[i];
                    count++;
                    if (getAdjacentRooms(currentArrowLocation).Contains(arrowRoom))
                    {
                        int originalArrowLocation = currentArrowLocation;
                        currentArrowLocation = arrowRoom;
                        if (count >= 2) //check to ensure no crooked arrows
                        {
                            while (currentArrowLocation == arrowTravellingRooms[count - 2]) //if crooked
                            {
                                currentArrowLocation = getAdjacentRooms(originalArrowLocation)[randomGenerator.Next(0, 2)];
                            }
                        }
                        arrowTravellingRooms[count] = currentArrowLocation;
                        Console.Write("" + arrowTravellingRooms[count] + " ");
                        if (CheckWumpusHit(arrowRoom))
                            currentGameState = Game1.State.PlayerWin;
                        if (CheckPlayerHit(currentArrowLocation)) //lose condition
                            currentGameState = Game1.State.PlayerHit;
                    }
                    else //pick adjacent room at random
                    {
                        if (count >= 2) //check to ensure no crooked arrows
                        {
                            int originalArrowLocation = currentArrowLocation;
                            do
                            {
                                currentArrowLocation = getAdjacentRooms(originalArrowLocation)[randomGenerator.Next(0, 2)];
                            } while (currentArrowLocation == arrowTravellingRooms[count - 2]);
                        }
                        else
                            currentArrowLocation = getAdjacentRooms(currentArrowLocation)[randomGenerator.Next(0, 2)];

                        arrowTravellingRooms[count] = currentArrowLocation;
                        Console.Write("" + arrowTravellingRooms[count] + " ");
                        if (CheckWumpusHit(currentArrowLocation))//win condition
                            currentGameState = Game1.State.PlayerWin;
                        //check if player hit
                        if (CheckPlayerHit(currentArrowLocation)) //lose condition
                            currentGameState = Game1.State.PlayerHit;
                    }

                }
                rooms = arrowTravellingRooms; //pass current arrow path
                Console.WriteLine("\n" + player.arrows + " arrows left.");
                if (wumpAwoken)
                {
                    Console.WriteLine("Wumpus was awakened!");
                    WumpusMove();
                    //EndTurn();
                }
                
            }

            else //if out of arrows then wumpus wins
                currentGameState = Game1.State.WumpWin;
        }

        //return true if wumpus was hit
        private bool CheckWumpusHit(int arrowLocation)
        {
            //check if wumpus was hit
            if (wump.pos == arrowLocation)
            {
                Console.WriteLine("You hunted the Wumpus! YOU WIN!");
                ReplayGame();
                return true;
            }
            return false;
        }

/// <summary>
/// Returns true if player was hit by arrow
/// </summary>
/// <param name="arrowLocation"></param>
/// <returns>bool</returns>
        private bool CheckPlayerHit(int arrowLocation)
        {
            //check if player was hit
            if (player.pos == arrowLocation)
            {
                Console.WriteLine("Arrow got you! YOU LOSE!");
                ReplayGame();
                return true;
            }
            return false;
        }

        public int[] getAdjacentRooms(int pos)
        {
                return Rooms[pos];

        }
        public void printHazards()
        {
            int[] adjacentRooms = getAdjacentRooms(player.pos);

            //check if location of wumpus is adjacent 
            if (adjacentRooms.Contains(wump.pos))
                Console.WriteLine("I smell a Wumpus nearby!");
            //check if pit nearby
            foreach (int p in Pits)
                if (adjacentRooms.Contains(p))
                    Console.WriteLine("I feel a draft - bottomless pit nearby");
            //check if bats are nearby
            foreach (int b in Bats)
                if (adjacentRooms.Contains(b))
                    Console.WriteLine("Bats nearby");            
                 
        }
        public String returnHazards()
        {
            int[] adjacentRooms = getAdjacentRooms(player.pos);
            string hazards = "";

            //check if location of wumpus is adjacent 
            if (adjacentRooms.Contains(wump.pos))
                hazards += "I smell a Wumpus nearby!\n";
            //check if pit nearby
            foreach (int p in Pits)
                if (adjacentRooms.Contains(p))
                    hazards += "Bottomless pit nearby\n";
            //check if bats are nearby
            foreach (int b in Bats)
                if (adjacentRooms.Contains(b))
                    hazards += "Bats nearby\n";
            return hazards;

        }
        private void InitRooms()
        {
            Rooms = new int[21][]; //ignore room 0, only look at 1-20
            //initialize adjacent rooms for each room
            Rooms[0] = new int[] { 0, 0, 0 }; //dummy room
            Rooms[1] = new int[] { 2, 5, 8 };
            Rooms[2] = new int[] { 1, 3, 10 };
            Rooms[3] = new int[] { 2, 4, 12 };
            Rooms[4] = new int[] { 3, 5, 14 };
            Rooms[5] = new int[] { 1, 4, 6 };
            Rooms[6] = new int[] { 5, 7, 15 };
            Rooms[7] = new int[] { 6, 8, 17 };
            Rooms[8] = new int[] { 1, 7, 9 };
            Rooms[9] = new int[] { 8, 10, 18 };
            Rooms[10] = new int[] { 2, 9, 11 };
            Rooms[11] = new int[] { 10, 12, 19 };
            Rooms[12] = new int[] { 3, 11, 13 };
            Rooms[13] = new int[] { 12, 14, 20 };
            Rooms[14] = new int[] { 4, 13, 15 };
            Rooms[15] = new int[] { 6, 14, 16 };
            Rooms[16] = new int[] { 15, 17, 20 };
            Rooms[17] = new int[] { 7, 16, 18 };
            Rooms[18] = new int[] { 9, 17, 19 };
            Rooms[19] = new int[] { 11, 18, 20 };
            Rooms[20] = new int[] { 13, 16, 19 };
        }
    }
}
