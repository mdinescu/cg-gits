using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{
    public static int MY_SIDE = 1;
    public static int ME = 1;
    public static int CPU = -1;
    public static string FACTORY = "FACTORY";
    public static string TROUP = "TROOP";
    public static string BOMB = "BOMB";

    
    class Link {
        public Link(int a, int b, int d) {
            A = a; B = b; Dist = d;
        }
        public int Dist { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        
        public override string ToString() {
            return String.Format("[{0}:{1} ({2})]", A, B, Dist);
        }
    }
    class Cell {
        public Cell(int id) {
            Id = id;
            Incoming = new List<Troup>();
            Reinforcements = new List<Troup>();
        }
        
        public int Id { get;set; }
        public List<Troup> Incoming { get; }
        public List<Troup> Reinforcements { get; } 
        public int Owner { get; set; }
        public int Troups { get; set; }
        public int Capacity { get; set; }
        public int Offline { get; set; }
        
        public void UpdateTroups(IEnumerable<Troup> mine, IEnumerable<Troup> opponent) {
            Reinforcements.AddRange(mine.Where(t => t.To == Id).OrderBy(t => t.Time));
            Incoming.AddRange(opponent.Where(t => t.To == Id).OrderBy(t => t.Time)); 
            if(Id == 8) {
                Console.Error.WriteLine(" {0} incoming and {1} friendly troops incoming to {2}", Incoming.Count, Reinforcements.Count, Id);
            }
        }
        public int CountIncoming() {            // troups that will be in this cell across all turns
            return Incoming.Sum(t => t.Size);
        }
        public int CountIncoming(int turn) {    // troups that will be in this cell by this turn
            return Incoming.Where(t => t.Time <= turn).Sum(t => t.Size);
        }
        public int CountReinforcements(int turn) {            // troups that will be in this cell across all turns
            return Reinforcements.Where(t => t.Time <= turn).Sum(t => t.Size);
        }

        public Cell PlayForward() {
            Cell cell = new Cell(Id) {
                Owner = this.Owner, Offline = Math.Max(this.Offline-1,0), Capacity = this.Capacity, Troups = this.Troups
            };
            
            // on next turn, increase production at factory first, if there is production
            if(cell.Owner != 0) {
                cell.Troups += cell.Capacity;
            }
            
            //if(cell.Id == 8) {
            //    Console.Error.WriteLine(": {0} play fwd -> {1} enemies and {2} friendlies incoming..", cell, Incoming.Count, Reinforcements.Count);
            //}
                
            int enemyCnt = 0;
            foreach(var oldTroup in Incoming) {
                var newTroup = oldTroup.Copy(true);
                if (newTroup.Time > 0) cell.Incoming.Add(newTroup);
                else enemyCnt += newTroup.Size;
            }
            int friendlyCnt = 0;
            foreach(var oldTroup in Reinforcements) {
                var newTroup = oldTroup.Copy(true);
                if (newTroup.Time > 0) cell.Reinforcements.Add(newTroup);
                else friendlyCnt += newTroup.Size;
            }

            if (enemyCnt > 0 || friendlyCnt > 0) {
                //if(cell.Id == 8) {
                //    Console.Error.WriteLine(": {0} has {1} enemies and {2} friendlies incoming..", cell, enemyCnt, friendlyCnt);
                //}
                int unitsLost = Math.Min(enemyCnt, friendlyCnt);            
                enemyCnt -= unitsLost; friendlyCnt -= unitsLost;
                
                if(cell.Owner == 0) { // neutral cell
                    if (friendlyCnt > cell.Troups) {
                        cell.Owner = ME; cell.Troups = friendlyCnt - cell.Troups;
                    }else if (enemyCnt > cell.Troups) {
                        cell.Owner = CPU; cell.Troups = enemyCnt - cell.Troups;
                    }
                }else if(cell.Owner == ME) { // friendly cell
                    if (friendlyCnt > 0) {
                        cell.Troups += friendlyCnt;
                    }else if(enemyCnt > cell.Troups) {
                        cell.Owner = CPU; cell.Troups = enemyCnt - cell.Troups;
                    }else {
                        cell.Troups = cell.Troups - enemyCnt;
                    }
                }else{ // enemy cell
                    if (enemyCnt > 0) {
                        cell.Troups += enemyCnt;
                    }else if(friendlyCnt > cell.Troups) {
                        cell.Owner = ME; cell.Troups = friendlyCnt - cell.Troups;
                    }else {
                        cell.Troups = cell.Troups - friendlyCnt;
                    }
                }
            } 
            return cell;         
        }

        public override string ToString() {
            return String.Format("C.{0}", Id);
        }
    }
    class Troup {
        public Troup(int id, int owner, int from, int to, int size, int time) {
            Id = id; Owner = owner; From = from; To = to; Size = size; Time = time;
        }
        public int Id { get;set; }
        public int Owner { get;set; }
        public int From { get;set; }
        public int To { get;set; }
        public int Size { get;set; }
        public int Time { get; set; }
        
        public Troup Copy(bool passTime) {
            return new Troup(Id, Owner, From, To, Size, passTime ? Math.Max(Time-1,0) : Time);
        }
        public override string ToString() {
            return String.Format("T({0} from {1} to {2})", Size, From, To);
        }
    }
    class Bomb {
        public Bomb(int id, int owner, int from) {
            Id = id; Owner = owner; From = from; Time = -1; To = -1;
        }
        public Bomb(int id, int owner, int from, int to, int time) {
            Id = id; Owner = owner; From = from; Time = time; To = to;
        }
        
        public int Id { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }
        public int Time { get; private set; }
        public int Delta { get; private set; }  // how many turns has this bomb been around
        public int Owner { get; private set; }

        public Bomb Copy(bool passTime) {
            var copy = new Bomb(Id, Owner, From, To, Time);
            copy.Delta = this.Delta + (passTime ? 1 : 0);
            copy.Time = Math.Max(-1, this.Time - (passTime ? 1 : 0));
            return copy;
        }
    }
    class Action {
        public Action (String verb, int from, int to) {
            Verb = verb; From = from; To = to;
        }
        public String Verb { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public int Count { get; set; }
        public static Action Move(int from, int to, int cnt) { return new Action("MOVE", from, to) { Count = cnt }; }
        public static Action Bomb(int from, int to) { return new Action("BOMB", from, to); }
        public static Action Inc(int id) { return new Action("INC", id, 0); }
        public override String ToString() { 
            if(Verb == "MOVE")
                return "MOVE " + From + " " + To + " " + Count; 
            else if(Verb == "BOMB")
                return "BOMB " + From + " " + To;
            else
                return "INC " + From;
        }
        
        public static String Combine(IEnumerable<Action> actions) {
            if(actions == null) return "WAIT;";
            var sb = new StringBuilder();
            foreach(var action in actions) {
                sb.Append(action.ToString());
                sb.Append(";");
            }
            return sb.ToString();
        }
    }
    
    class Map {
        private int[][] NEXT;
        private int[][] SP_D;
        private int[][] DIST;

        public Map(int size) {
            DIST = new int[size][]; SP_D = new int[size][]; NEXT = new int[size][];
            for(int i = 0; i < size; i++) {
                DIST[i] = new int[size]; SP_D[i] = new int[size]; NEXT[i] = new int[size];                
            }
        }
        public static Map Parse(TextReader tr) {
            string[] inputs;
            Map map = new Map(int.Parse(tr.ReadLine()));
            int linkCount = int.Parse(tr.ReadLine()); // the number of links between factories
            for (int i = 0; i < linkCount; i++)
            {
                inputs = tr.ReadLine().Split(' ');            
                int f1 = int.Parse(inputs[0]);
                int f2 = int.Parse(inputs[1]);
                int dist = int.Parse(inputs[2]);
                map.DIST[f1][f2] = dist;
                map.DIST[f2][f1] = dist;
                map.SP_D[f1][f2] = map.DIST[f1][f2];
                map.SP_D[f2][f1] = map.DIST[f2][f1];
                map.NEXT[f1][f2] = f2;
            }
            return map;
        }

        public int ShortestDist(int c1, int c2) {
            return SP_D[c1][c2];
        }
        public int Dist(int c1, int c2) {
            return DIST[c1][c2];
        }
        public int Size { get { return DIST.Length; } }

        public void ComputeShortestPaths() {
            int N = Size; int MXX = int.MaxValue;
            for(int i = 0; i < N; i++) {
                for(int j = 0; j < N; j++) {                    
                        SP_D[i][j] = DIST[i][j];
                        NEXT[i][j] = j;
                    //Console.Error.Write("{0:D3} ", NEXT[i][j]);
                }
                Console.Error.WriteLine();
            }
            
            for(int k = 0; k < N; k++) {
                for(int i = 0; i < N; i++) {
                    for(int j = 0; j < N; j++) {
                        int d = (SP_D[i][k] != MXX && 
                                SP_D[k][j] != MXX)
                                ? SP_D[i][k] + SP_D[k][j] + 1       // each hop adds 1 turn
                                : MXX;
                        if(d <= SP_D[i][j]) {                       // if paths equal, prefer the one w/ extra hops
                            SP_D[i][j] = d;
                            NEXT[i][j] = NEXT[i][k];
                        }
                    }
                }
            }
        }

        public List<Link> ShortestPath(int c1, int c2) {
            var path = new List<Link>();
            int cc = c1;
            while(cc != c2) {         
                int nc = NEXT[cc][c2];
                path.Add(new Link(cc, nc, DIST[cc][nc]));
                cc = nc;           
            }
            return path;
        }
    }

    class Context {
        public Context(Map map) {
            Cells = new Cell[map.Size]; Bombs = new List<Bomb>(2);
            playerCells = new List<Cell>[3];
            playerCells[0] = new List<Cell>(map.Size);
            playerCells[1] = new List<Cell>(map.Size);
            playerCells[2] = new List<Cell>(map.Size);
            for(int i = 0; i < map.Size; i++)
                Cells[i] = new Cell(i);
            MyTroups = new List<Troup>();
            EnemyTroups = new List<Troup>();
        }
        public static Context Parse(TextReader tr, Context previous, Map map) {
            string[] inputs;
            Context ctx = new Context(map);
            if(previous != null) {
                ctx.Turn = previous.Turn + 1;
                ctx.posPlayerBombsLeft = previous.posPlayerBombsLeft;
                ctx.negPlayerBombsLeft = previous.negPlayerBombsLeft;
            } else {
                ctx.posPlayerBombsLeft = 2;
                ctx.negPlayerBombsLeft = 2;
            }
                        
            int entityCount = int.Parse(tr.ReadLine());
            for (int i = 0; i < entityCount; i++)
            {
                inputs = tr.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]); string entityType = inputs[1];
                int arg1 = int.Parse(inputs[2]); int arg2 = int.Parse(inputs[3]);
                int arg3 = int.Parse(inputs[4]); int arg4 = int.Parse(inputs[5]);
                int arg5 = int.Parse(inputs[6]);                
                
                if(entityType == FACTORY) {
                    ctx.Cells[entityId].Owner = arg1;
                    ctx.Cells[entityId].Troups = arg2;
                    ctx.Cells[entityId].Capacity = arg3;
                    ctx.Cells[entityId].Offline = arg4;
                    
                    ctx.playerCells[arg1 + 1].Add(ctx.Cells[entityId]);                    
                    //Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);                     
                }else if(entityType == TROUP) {
                    var trp = new Troup(entityId, arg1, arg2, arg3, arg4, arg5);                     
                    if(trp.Owner == ME) {
                        ctx.MyTroups.Add(trp);                        
                    }else {
                        ctx.EnemyTroups.Add(trp);
                    }
                }else {                
                    Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);
                    var bmb = new Bomb(entityId, arg1, arg2, arg3, arg4);
                    var sameBomb = previous.Bombs.Where(b => b.Id == bmb.Id).FirstOrDefault();
                    if(sameBomb != null) { 
                        if(sameBomb.From != bmb.From) Console.Error.WriteLine("!!!ERROR same bomb {0}: from {1} != {2}", bmb.Id, bmb.From, sameBomb.From);
                        ctx.Bombs.Add(sameBomb.Copy(true)); // add the one from previous turn because we want to keep track of the Delta
                    }else {
                        ctx.Bombs.Add(bmb);
                        if (bmb.Owner == ME) {
                            ctx.posPlayerBombsLeft--;
                        } else {
                            ctx.negPlayerBombsLeft--;
                        }
                    }                                      
                }
            }
            
            foreach(var cell in ctx.Cells) {            
                cell.UpdateTroups(ctx.MyTroups, ctx.EnemyTroups);
            }
            ctx.UpdateScore();
            return ctx;
        }

        public void UpdateScore() {
            posPlayerScore = 0; negPlayerScore = 0;
                      
            for (int i = 0; i < Cells.Length; i++) {
                if(Cells[i].Owner == ME) posPlayerScore += Cells[i].Troups;
                else if(Cells[i].Owner == CPU) negPlayerScore += Cells[i].Troups;  
            }

            posPlayerScore += MyTroups.Sum(t => t.Size);
            negPlayerScore += EnemyTroups.Sum(t => t.Size);

            Console.Error.WriteLine(" -> updating scores:  {0} vs {1} on turn {2}", posPlayerScore, negPlayerScore, Turn);      
        }

        private int posPlayerScore; private int negPlayerScore;
        private int posPlayerBombsLeft; private int negPlayerBombsLeft;
        public int Turn { get; private set; }
        public Cell[] Cells { get; private set; }
        public List<Bomb> Bombs { get; private set; }
        public IEnumerable<Bomb> GetBombs(int playerId) { return Bombs.Where(b => b.Owner == playerId); }        
        public List<Troup> MyTroups { get; private set; }
        public List<Troup> EnemyTroups { get; private set; }         
        public int GetScore(int playerId) { return playerId == 1 ? posPlayerScore : negPlayerScore; }  
        public int GetBombsLeft(int playerId) { return playerId == 1 ? posPlayerBombsLeft : negPlayerBombsLeft; }      
        private List<Cell>[] playerCells;
        public List<Cell> GetPlayerCells(int playerId) {
            return playerCells[playerId+1];
        }

        public Cell GetBestPathFwd(int from, int to, Map map) {
            var sp = map.ShortestPath(from, to); 
            var nextHop = Cells[sp[0].B];
            return nextHop;
            if (nextHop.Owner == ME) return nextHop;
            if (sp.Count > 1) {            
                foreach(var link in sp) {
                    nextHop = Cells[link.B];
                    if (nextHop.Owner == ME) return nextHop;
                    if (nextHop.Capacity == 0 && nextHop.Troups < 2) return nextHop;
                    if (nextHop.Capacity * map.ShortestDist(from, link.B) > 3)
                        continue;                
                }   
            }
            return Cells[to];
        }
    }
    
    static void Main(string[] args)
    {
        Map map = Map.Parse(Console.In);
        map.ComputeShortestPaths();

        Console.Error.WriteLine("-------------------------"); 
        
        List<Troup> myTroups = new List<Troup>();
        List<Troup> enemyTroups = new List<Troup>();

        int myScore = 0; int cpuScore = 0;

        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        Context previousCtx = null;

        var missleDefenseStgy = new MissleDefenseStrategy(map);
        var missleOffenseStgy = new MissleOffenseStrategy(map);
        while (true)
        {
            Context ctx = Context.Parse(Console.In, previousCtx, map);
            stopWatch.Restart();

            if(ctx.Turn == 0) {
                MY_SIDE = ctx.Cells.Where(c => c.Owner == ME).First().Id % 2; 
                Console.Error.WriteLine("I'm on " + (MY_SIDE != 0 ? "LEFT" : "RIGHT"));            
            }
            
            Console.Error.WriteLine("----[ {0:D3} ]  vs   [ {1:D3} ]---", ctx.GetScore(ME), ctx.GetScore(-ME));
            var myCells = ctx.GetPlayerCells(ME);
            var neutCells = ctx.GetPlayerCells(0);
            var enemyCells = ctx.GetPlayerCells(CPU);            
            
            // --------------- INITIALIZE ACTIONS FOR THIS TURN ----------------------------------------
            var actions = new Dictionary<int, List<Action>>();            
            foreach(var myC in myCells) {
                actions.Add(myC.Id, new List<Action>());                
            }
            
            // --------------- LOOK FOR CELLS TO DEFEND/ATTACK ----------------------------------------
            var availableTroups = new int[ctx.Cells.Length];
            var fightFlight = new Dictionary<int, FightFlight>();
            var needsHelp = new Dictionary<Cell, int>();
            var freeToAttack = new HashSet<int>();
            Console.Error.WriteLine(" -- CELL DEFENSE --");
            foreach(var myC in myCells) {
                availableTroups[myC.Id] = myC.Troups; // total available troups for each of my cells

                Cell futureCell = myC;                
                Console.Error.WriteLine("{0} troups= {1}, cap= {2}", myC, myC.Troups, myC.Capacity);
                                            
                for(int i = 0; i < 5; i++) {  // see what things will look like in 5 future turns
                    futureCell = futureCell.PlayForward();                
                    if(futureCell.Owner == ME) {
                        //if (i == 0) availableTroups[myC.Id] = futureCell.Troups;
                        // still mine.. 
                        if (futureCell.Capacity == 0 && futureCell.Troups < myC.Troups) {
                            freeToAttack.Remove(myC.Id);
                            fightFlight.Add(myC.Id, FightFlight.SolveFor(ctx, map, myC, i));
                            break;
                        } else {
                            //Console.Error.WriteLine("free to attack as of {1} turns", myC, i); 
                            freeToAttack.Add(myC.Id);   // TODO: ?? this should be improved.. need to look at incomings and determine excess troups
                        }
                    }else {
                        // needs defense
                        freeToAttack.Remove(myC.Id);    // TODO: ????

                        Console.Error.WriteLine("{0} needs help in {1} turns", myC, i); 
                        
                        int totalHelpAvailable = myCells.Where(c => c.Id != myC.Id && map.Dist(c.Id, myC.Id) <= i)
                                                        .Sum(c => c.Troups + c.Capacity * map.Dist(c.Id, myC.Id));
                        
                        if(myC.Capacity > 0 && totalHelpAvailable > futureCell.Troups) {                            
                            needsHelp.Add(futureCell, i);
                            break;
                        }else {                    
                            fightFlight.Add(myC.Id, FightFlight.SolveFor(ctx, map, myC, i));
                            break;
                        }
                    }
                }
            }            

            foreach(var needsHelpKV in needsHelp.OrderBy(kv => -kv.Key.Capacity).ThenBy(kv => kv.Value)) {
                var needHelpCell = needsHelpKV.Key;
                int troupsNeeded = needHelpCell.Troups;
                foreach(var ff in fightFlight.Values.Where(ff => map.Dist(ff.Cell.Id, needHelpCell.Id) <= needsHelpKV.Value)) {
                    if(availableTroups[ff.Cell.Id] >= troupsNeeded) {
                        var nextHop = ctx.GetBestPathFwd(ff.Cell.Id, needHelpCell.Id, map);
                        actions[ff.Cell.Id].Add(Action.Move(ff.Cell.Id, nextHop.Id, troupsNeeded));
                        troupsNeeded = 0;
                        break;
                    }else {
                        var nextHop = ctx.GetBestPathFwd(ff.Cell.Id, needHelpCell.Id, map);
                        actions[ff.Cell.Id].Add(Action.Move(ff.Cell.Id, nextHop.Id, availableTroups[ff.Cell.Id]));
                        troupsNeeded -= availableTroups[ff.Cell.Id];
                    }                    
                }
                if (troupsNeeded > 0) {
                    foreach(var cell in ctx.Cells.Where(c => freeToAttack.Contains(c.Id)).OrderBy(c => map.Dist(c.Id, needHelpCell.Id))) {
                        if(availableTroups[cell.Id] >= troupsNeeded) {
                            var nextHop = ctx.GetBestPathFwd(cell.Id, needHelpCell.Id, map);
                            actions[cell.Id].Add(Action.Move(cell.Id, nextHop.Id, troupsNeeded));
                            troupsNeeded = 0;
                            break;
                        }else {
                            var nextHop = ctx.GetBestPathFwd(cell.Id, needHelpCell.Id, map);
                            actions[cell.Id].Add(Action.Move(cell.Id, nextHop.Id, availableTroups[cell.Id]));
                            troupsNeeded -= availableTroups[cell.Id];
                        } 
                    }
                }
            }

            foreach(var ff in fightFlight.Values) {
                var fightFlightSolution = ff.Choose(true);
                if (fightFlightSolution != null) {
                    var nextHop = ctx.GetBestPathFwd(ff.Cell.Id, fightFlightSolution.Id, map);
                    actions[ff.Cell.Id].Add(Action.Move(ff.Cell.Id, nextHop.Id, availableTroups[ff.Cell.Id]));
                }
            }
            
            Console.Error.WriteLine(" -- CELL ACTIONS --");
            foreach(var myC in myCells) {                
                int reserve = myC.Capacity;
                int troupsLeft = availableTroups[myC.Id];
                int futureTroups = troupsLeft;                                                
                Console.Error.WriteLine("{0} troups= {1}, cap= {2}", myC, troupsLeft, myC.Capacity);            
                if(freeToAttack.Contains(myC.Id)) {
                    /*
                    foreach(var b in ctx.GetBombs(ME)) {
                        bool hasTroup = ctx.MyTroups.Any(t => t.To == b.To && t.Time == b.Time + 1 || t.Time == b.Time + 2);
                        int distToBombTarget = map.Dist(myC.Id, b.To);
                        if (!hasTroup && availableTroups[myC.Id] > 1 && distToBombTarget == b.Time + 1) {
                            availableTroups[myC.Id]--;
                            actions[myC.Id].Add(Action.Move(myC.Id, b.To, 1));
                        }
                    }
                    */
                    var interestingCells = ctx.Cells
                                            .Where(c => c.Owner != ME 
                                                    //&& map.Dist(c.Id, myC.Id) < 10
                                                    && c.Troups < futureTroups && c.Capacity > 0)
                                            .OrderBy(c => -c.Capacity)
                                            .ThenBy(c => map.ShortestDist(myC.Id, c.Id))
                                            .ThenBy(c => c.Troups);
                    
                    int holdBack = 0; // should be based on how much I need for defense  myC.Capacity - ic.Capacity;
                        
                    int sendingAway = 0;
                    foreach(var ic in interestingCells) {                        
                        var icStr = String.Format("  {0} troups= {1}, cap= {2} -> ", ic, ic.Troups, ic.Capacity); 
                        
                        if(ctx.GetBombs(ME).Any(b => b.To == ic.Id && b.Time > map.ShortestDist(myC.Id, ic.Id))) {
                            Console.Error.WriteLine(icStr + " ignore: bomb coming");
                            break;
                        }
                        
                        int extra = ic.Owner == CPU ? ic.Capacity * (1+map.ShortestDist(myC.Id, ic.Id)) + 1 : 1;
                        
                        if(ic.Troups + holdBack + extra < futureTroups) {
                            var nextHop = ctx.GetBestPathFwd(myC.Id, ic.Id, map);                    
                            Console.Error.WriteLine(icStr + " attack with {0} via {1}", ic.Troups + extra, nextHop.Id);                                                     
                            actions[myC.Id].Add(Action.Move(myC.Id, nextHop.Id, ic.Troups + extra));
                            futureTroups -= (ic.Troups + extra);
                            sendingAway += ic.Troups + extra;
                        }else {
                            Console.Error.WriteLine(icStr + " ignore: needed {0}; not enough", ic.Troups + extra);
                        }
                        if(futureTroups <= reserve || myC.Troups - sendingAway <= 0) {
                            break;
                        }
                    }
                    
                    Console.Error.Write(" {0} troups left..", futureTroups);
                    if(ctx.GetBombs(-ME).Count() == 0) {
                        
                        if(myC.Troups - sendingAway >= 10 && myC.Capacity < 3) {                        
                            int defenceCapacity = (myC.Troups - sendingAway - 10); bool canDefend = true;
                            for(int turn = 1; turn < 3; turn++) {  // TODO:  should I look at 3 turns in the future?!
                                int attackCapacity = ctx.GetPlayerCells(CPU)
                                                        .Where(c => map.Dist(c.Id, myC.Id) == turn)
                                                        .Sum(c => c.Troups + c.Capacity);
                                attackCapacity += myC.Incoming.Where(it => it.Time == turn).Sum(it => it.Size);
                                defenceCapacity += myC.Capacity + 1 + myC.Reinforcements.Where(rt => rt.Time == turn).Sum(rt => rt.Size);
                                
                                defenceCapacity -= attackCapacity;
                                if(defenceCapacity < 0) {
                                    Console.Error.WriteLine(" chk upgrade failed; defence fails at turn " + turn);
                                    canDefend = false;
                                    break;
                                }
                            }
                            if(canDefend) {
                                Console.Error.WriteLine(" chk upgrade success! -> updateding");
                                actions[myC.Id].Add(Action.Inc(myC.Id));
                            }
                        }else {
                            Console.Error.WriteLine(" chk upgrade failed; " + ((myC.Capacity < 3) ? "not enough troups left" : "already max"));
                        }
                    }else{
                        Console.Error.WriteLine(" chk upgrade failed; boms in flight");
                    }
                }
            }
            
            if(ctx.GetBombs(-ME).Count() > 0) {                
                Console.Error.WriteLine(" -----  MISSLE DEFENSE ----- ");
                var mdsActions = missleDefenseStgy.Apply(ctx, ME); // apply missle defense, from ME perspective
                foreach(var action in mdsActions) {
                    actions[action.Key] = action.Value;
                }
            }
                        
            Console.Error.WriteLine(" -----  MISSLE OFFENSE ----- ");
            var mosActions = missleOffenseStgy.Apply(ctx, ME);
            foreach(var action in mosActions) {
                actions[action.Key].AddRange(action.Value);
            }

            string msg = "(" + stopWatch.Elapsed.TotalMilliseconds + ")";
            stopWatch.Reset();            
            
            foreach(var aList in actions) {
                Console.Error.Write(aList.Key + ": ");
                foreach(var a in aList.Value) {
                    Console.Error.Write(a + " ");
                }
                Console.Error.WriteLine();
            }

            var output = Action.Combine(actions.Values.SelectMany(v => v));
            output += "MSG " + msg;
            Console.WriteLine(output);
            previousCtx = ctx;
        }
    }

    class FightFlight {
        public Cell Cell { get; set; }
        public List<Cell> Fight { get; set; }
        public List<Cell> Flee { get; set; }
        public int TurnsLeft { get; set; }
        public Cell Choose(bool preferAttack) {
            if(preferAttack && Fight.Count > 0) return Fight[0];
            else if(Flee.Count > 0) return Flee[0];            
            else if(Fight.Count > 0) return Fight[0];
            return null;
        }

        public static FightFlight SolveFor(Context ctx, Map map, Cell cell, int turnsLeft) {
            Console.Error.Write("{0} fight/flight in {1} turns ({2} capacity)", cell, turnsLeft, cell.Capacity);
            var canAttack = "";
            var fights = new List<Cell>();
            foreach(var eCell in ctx.GetPlayerCells(CPU)
                                    .Where(ec => ec.Troups + ec.Capacity * map.Dist(ec.Id, cell.Id) < cell.Troups && ec.Capacity > 0)
                                    .OrderBy(ec => map.Dist(ec.Id, cell.Id))
                                    .ThenBy(ec => -ec.Capacity)) {
                canAttack += eCell.Id + ";";
                fights.Add(eCell);
            }
            Console.Error.WriteLine(" -> attack: " + canAttack);
            var canFlee = "";
            var flees = new List<Cell>();
            foreach(var fCell in ctx.GetPlayerCells(ME)
                                    .Where(fc => fc.Id != cell.Id)
                                    .OrderBy(fc => map.Dist(fc.Id, cell.Id))
                                    .ThenBy(fc => -fc.Capacity)) {
                canFlee += fCell.Id + ";";
                flees.Add(fCell);
            }
            Console.Error.WriteLine(" -> or flee: " + canFlee);

            return new FightFlight() { Cell = cell, Fight = fights, Flee = flees, TurnsLeft = turnsLeft };
        }
    }

    private static Cell EvaluateOwnership(Cell cell, int turns) {
        //if(cell.Id == 8) 
        //    Console.Error.WriteLine(": eval {0} in {1} turns", cell, turns);
        Cell lastState = cell;
        for(int i = 0; i < turns; i++) {
            var newState = lastState.PlayForward();
            lastState = newState;            
        }
        //if(cell.Id == 8)
        //    Console.Error.WriteLine(":: {0} after {1} turns will be owned by {2} and have {3} troups", cell, turns, lastState.Owner, lastState.Troups);
        return lastState;
    }

    class Strategy {
        protected void AddAction(Dictionary<int, List<Action>> actions, int cellId, Action a, bool clear) {
            if(!actions.ContainsKey(cellId)) actions.Add(cellId, new List<Action>());
            if(clear) actions[cellId].Clear();
            actions[cellId].Add(a);
        }
    }

    class MissleDefenseStrategy : Strategy {
        private Map map;
        public MissleDefenseStrategy(Map map) {
            this.map = map;
        }
        public Dictionary<int, List<Action>> Apply(Context ctx, int playerId) {
            var actions = new Dictionary<int, List<Action>>();

            var myCells = ctx.GetPlayerCells(playerId);            
            var neutralCells = ctx.GetPlayerCells(0);
            var enemyCells = ctx.GetPlayerCells(-playerId);
            
            // -- check if any of the enemy bombs could be headed to each cell 
            var possibleBombTargets = new HashSet<int>();
            foreach(var eb in ctx.GetBombs(-playerId)) {
                //Console.Error.WriteLine("targets for {0}, away for {1}", eb.Id, eb.Delta);
                foreach(var cell in myCells) {
                    int dist = map.Dist(cell.Id, eb.From);
                    if (dist == eb.Delta + 1) {
                        Console.Error.WriteLine("potential bomb target {0}", cell.Id);
                        possibleBombTargets.Add(cell.Id);
                    }
                }
            }
            foreach(var eb in ctx.GetBombs(-playerId)) {
                foreach(var possibleTarget in possibleBombTargets) {
                    var myC = ctx.Cells[possibleTarget];
                    int dist = map.Dist(myC.Id, eb.From);                        
                    if (dist == eb.Delta + 1) {
                        // check if any of my own are taking fire.. and direct the units there
                        Cell safeCell = null;
                        if (myC.Capacity < 2) {
                            safeCell = myCells.Where(c => !possibleBombTargets.Contains(c.Id) 
                                                        && map.Dist(c.Id, eb.From) != eb.Delta + map.Dist(c.Id, myC.Id) 
                                                        && c.Capacity >= myC.Capacity
                                                        ).OrderBy(c => -c.Incoming.Sum(t => t.Size))
                                                        .OrderBy(c => map.Dist(c.Id, myC.Id)).FirstOrDefault();                            
                        }
                        if(safeCell == null) {
                            safeCell = myCells.Where(c => !possibleBombTargets.Contains(c.Id) && map.Dist(c.Id, eb.From) < dist).OrderBy(c => map.Dist(c.Id, myC.Id)).FirstOrDefault();
                            if (safeCell == null) {
                                // try to find one that is neutral and convenient
                                safeCell = neutralCells.Where(c => map.Dist(c.Id, eb.From) != dist && c.Troups <= myC.Troups + myC.Capacity).OrderBy(c => -c.Capacity).FirstOrDefault();
                                if (safeCell == null) {
                                    safeCell = myCells.Where(c => !possibleBombTargets.Contains(c.Id) && map.Dist(c.Id, eb.From) > dist + 1).OrderBy(c => map.Dist(c.Id, myC.Id)).FirstOrDefault();
                                    if(safeCell == null) {
                                        // still nothing.. then look for one of the enemy ones
                                        safeCell = enemyCells.Where(c => c.Troups + c.Capacity * map.Dist(c.Id, myC.Id) <= myC.Troups + myC.Capacity).OrderBy(c => -c.Capacity).FirstOrDefault();
                                        if (safeCell == null) {
                                            safeCell = enemyCells.OrderBy(c => (c.Troups + c.Capacity * map.Dist(c.Id, myC.Id)) - (myC.Troups + myC.Capacity)).FirstOrDefault();                                        
                                        }
                                    }
                                }
                            }
                        }
                        if (safeCell != null) {
                            //Console.Error.WriteLine("- found safe cell {0}", safeCell);
                            var nextHop = ctx.GetBestPathFwd(myC.Id, safeCell.Id, map);
                            AddAction(actions, myC.Id, Action.Move(myC.Id, nextHop.Id, myC.Troups + myC.Capacity), true);                            
                        }                            
                    }
                }
            }
            return actions;
        }
    }

    class OldMissleOffenseStrategy : Strategy {
        private Map map;
        public OldMissleOffenseStrategy(Map map) {
            this.map = map;
        }

        public Dictionary<int, List<Action>> Apply(Context ctx, int playerId) {
            var actions = new Dictionary<int, List<Action>>();

            int bombsLeft = ctx.GetBombsLeft(playerId);
            if (bombsLeft == 0) return actions;

            var myOtherBomb = ctx.GetBombs(playerId).FirstOrDefault();
            if( myOtherBomb != null) {
                Console.Error.WriteLine("bombing {0} from {1} in {2} turns", myOtherBomb.To, myOtherBomb.From, myOtherBomb.Time);
            }
            bool newBomb = ctx.GetBombs(-playerId).Any(b => b.Delta == 0); // there's at least one enemy bomb launched this turn!   
            if(newBomb) { 
                var bomberDest = ctx.GetPlayerCells(-playerId).Where(oc => oc.Offline == 0
                                                    && (myOtherBomb == null || myOtherBomb.To != oc.Id))
                                            .OrderBy(oc => -oc.Capacity)
                                            .FirstOrDefault();
                if(bomberDest != null) {
                    Console.Error.WriteLine(".. bmb dest = " + bomberDest);
                    var bomber = ctx.GetPlayerCells(playerId)
                                //.Where(c => c.Troups == 0)
                                .OrderBy(c => map.Dist(c.Id, bomberDest.Id))
                                .FirstOrDefault();
                    if(bomber != null) {                            
                        Console.Error.WriteLine(".. bmber = " + bomber);
                        AddAction(actions, bomber.Id, Action.Bomb(bomber.Id, bomberDest.Id), false);                        
                        bombsLeft--;                            
                    }
                }
            }

            if (bombsLeft > 0) {
                Cell firstBombSite = null;
                // if I have any bombs left, look through potential destinations
                firstBombSite = ctx.GetPlayerCells(-playerId)
                                    .Where(c => (c.Capacity == 3 || (c.Troups >= 10 && c.Capacity > 1))
                                            && c.Offline == 0
                                            && (myOtherBomb == null || myOtherBomb.To != c.Id))
                                    .OrderBy(c => -c.Capacity).ThenBy(c => -c.Troups)
                                    .FirstOrDefault();
                
                if(firstBombSite != null) {
                    Console.Error.WriteLine(" [CONSIDER THE BOMB TO: " + firstBombSite + "]");
                    
                    var bombFrom = ctx.GetPlayerCells(playerId).OrderBy(c => map.Dist(c.Id, firstBombSite.Id))                                                                       
                                    .FirstOrDefault();
                    Console.Error.WriteLine("consider to bomb from " + bombFrom.Id);
                    AddAction(actions, bombFrom.Id, Action.Bomb(bombFrom.Id,firstBombSite.Id), false);                    
                    bombsLeft--;
                    
                    Console.Error.WriteLine("First choice for bombing: {0}", firstBombSite);
                }                
            }
            return actions;
        }
    }

    class MissleOffenseStrategy : Strategy {
        private Map map;
        public MissleOffenseStrategy(Map map) {
            this.map = map;
        }

        public Dictionary<int, List<Action>> Apply(Context ctx, int playerId) {
            var actions = new Dictionary<int, List<Action>>();

            int bombsLeft = ctx.GetBombsLeft(playerId);
            if(bombsLeft == 0) return actions;

            var myOtherBomb = ctx.GetBombs(playerId).FirstOrDefault();
            if (myOtherBomb != null) {
                Console.Error.WriteLine("bombing {0} from {1} in {2} turns", myOtherBomb.To, myOtherBomb.From, myOtherBomb.Time);
            }
            bool newBomb = ctx.GetBombs(-playerId).Any(b => b.Delta == 0); // there's at least one enemy bomb launched this turn!   

            Console.Error.WriteLine("Looking for a bomb stgy (newBomb == {0})", newBomb);                    

            // first aim for the largest enemy factory by production and # of troups
            //  +
            //    int damageScore = Math.Min(cell.Troups, Math.Max(10, cell.Troups / 2)) + cell.Capacity * 5;

            // look for neutral cells that would be taken by enemy by the time the bomb is deployed
            //  -
            //    int sacrificedTroups = sum(troups-in-transit-to-target, where arrival time == explosion-time)
                                
            // evalOwnership(cell, at-arrival, based on troups in trasit)
            var bombSites = new Dictionary<Tuple<Cell, Cell>, int>();
            foreach(var bomber in ctx.GetPlayerCells(playerId).Where(c => myOtherBomb == null || myOtherBomb.From != c.Id)) {
                // based on the selected bomber..
                var bombSite = ctx.Cells.Where(c => c.Owner != playerId && (myOtherBomb == null || myOtherBomb.To != c.Id))
                            .Select(c => EvaluateOwnership(c, map.Dist(c.Id, bomber.Id)))
                            .Where(c => c.Owner == CPU && c.Offline == 0 && c.Capacity > 0)
                            .OrderBy(c => -c.Capacity * 5) // (Math.Min(c.Troups, Math.Max(10, c.Troups / 2)) + 
                            .FirstOrDefault();
                if(bombSite == null) continue; 
                bombSites.Add(Tuple.Create(bombSite, bomber), Math.Min(bombSite.Troups, Math.Max(10, bombSite.Troups / 2)) + bombSite.Capacity * 5);
            }

            foreach(var bombSolution in bombSites.OrderBy(kv => map.Dist(kv.Key.Item1.Id, kv.Key.Item2.Id))) {
                // pick the best one out of all options                
                var bomber = bombSolution.Key.Item2;
                var bombSite = bombSolution.Key.Item1;

                Console.Error.WriteLine(".. bmb site = " + bombSite);
                Console.Error.WriteLine(".. bmber = " + bomber);
                AddAction(actions, bomber.Id, Action.Bomb(bomber.Id, bombSite.Id), false);
                bombsLeft--;
                if (bombsLeft == 0) break;                
            }
            
            return actions;
        }
    }
}
