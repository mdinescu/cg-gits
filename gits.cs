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
            Outgoing = new List<Troup>();
        }
        
        public int Id { get;set; }
        public List<Troup> Incoming { get; }
        public List<Troup> Reinforcements { get; }
        public List<Troup> Outgoing { get; } 
        public int Owner { get; set; }
        public int Troups { get; set; }
        public int Capacity { get; set; }
        public int Offline { get; set; }
        
        public void UpdateTroups(IEnumerable<Troup> mine, IEnumerable<Troup> opponent) {
            Reinforcements.AddRange(mine.Where(t => t.To == Id).OrderBy(t => t.Time));
            Incoming.AddRange(opponent.Where(t => t.To == Id).OrderBy(t => t.Time));
            Outgoing.AddRange(mine.Where(t => t.From == Id).OrderBy(t => t.Time));            
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

        public override string ToString() {
            return String.Format("C.{0}", Id);
        }
    }
    class Troup {
        public Troup(int id) {
            Id = id;
        }
        public int Id { get;set; }
        public int Owner { get;set; }
        public int From { get;set; }
        public int To { get;set; }
        public int Size { get;set; }
        public int Time { get; set; }
        
        public override string ToString() {
            return String.Format("T({0} from {1} to {2})", Size, From, To);
        }
    }
    class Bomb {
        public Bomb(int id, int owner, int from, int time, int to) {
            Id = id; Owner = owner; From = from; Time = time; To = to; OriginalTime = time;
        }
        public Bomb(int id, int owner, int from, int time) {
            Id = id; Owner = owner; From = from; Time = time; To = -1; OriginalTime = time;
        }
        
        public int Id { get; private set; }
        public int From { get; private set; }
        public int To { get; private set; }
        public int Time { get; private set; }
        public int OriginalTime { get; private set; }
        public int Owner { get; private set; }

        public Bomb Copy(bool passTime) {
            var copy = new Bomb(Id, Owner, From, Time - (passTime ? 1 : 0), To);
            copy.OriginalTime = this.OriginalTime;
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
                                ? SP_D[i][k] + SP_D[k][j]
                                : MXX;
                        if(d < SP_D[i][j]) {
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
                ctx.MyBombsLeft = previous.MyBombsLeft;
                ctx.EnemyBombsLeft = previous.EnemyBombsLeft;
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
                    Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);                     
                }else if(entityType == TROUP) {
                    var trp = new Troup(entityId) { Owner = arg1, From = arg2, To = arg3, Size = arg4, Time = arg5 };                    
                    if(trp.Owner == ME) {
                        ctx.MyTroups.Add(trp);                        
                    }else {
                        ctx.EnemyTroups.Add(trp);
                    }
                }else {                
                    Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);
                    var bmb = new Bomb(entityId, arg1, arg2, arg4, arg3);
                    var sameBomb = previous.Bombs.Where(b => b.Id == bmb.Id).FirstOrDefault();
                    if(sameBomb != null) { 
                        if(sameBomb.Time != bmb.Time) Console.Error.WriteLine("!!!ERROR same bomb {0} time {1} != {2}", bmb.Id, bmb.Time, sameBomb.Time);
                        ctx.Bombs.Add(sameBomb.Copy(true)); // add the one from previous turn because we want to know the OriginalTime
                    }else {
                        ctx.Bombs.Add(bmb);
                        if (bmb.Owner == ME) {
                            ctx.MyBombsLeft--;
                        } else {
                            ctx.EnemyBombsLeft--;
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
            int myScore = 0; int enemyScore = 0;
            for (int i = 0; i < Cells.Length; i++) {
                if(Cells[i].Owner == ME) myScore += Cells[i].Troups;
                else if(Cells[i].Owner == CPU) enemyScore += Cells[i].Troups;  
            }

            myScore += MyTroups.Sum(t => t.Size);
            enemyScore += EnemyTroups.Sum(t => t.Size);

            MyScore = myScore;
            EnemyScore = enemyScore;
        }

        public Cell[] Cells { get; private set; }
        public List<Bomb> Bombs { get; private set; }
        public IEnumerable<Bomb> MyBombs { get { return Bombs.Where(b => b.Owner == ME); } }
        public IEnumerable<Bomb> EnemyBombs { get { return Bombs.Where(b => b.Owner != ME); } }
        public List<Troup> MyTroups { get; private set; }
        public List<Troup> EnemyTroups { get; private set; }
        public int MyScore { get; private set; }
        public int EnemyScore { get; private set; }
        public int Turn { get; private set; }

        public int MyBombsLeft { get; private set; }
        public int EnemyBombsLeft { get; private set; }

        public Cell GetBestPathFwd(int from, int to, Map map) {
            var sp = map.ShortestPath(from, to); 
            var nextHop = Cells[sp[0].B];
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
        var incomingT = new Dictionary<int, List<Troup>>();
        var outgoingT = new Dictionary<int, List<Troup>>();
        
        var myBombs = new List<Bomb>();
        var enemyBombs = new List<Bomb>();
        
        int myScore = 0; int cpuScore = 0;

        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        Context previousCtx = null;
        while (true)
        {
            Context ctx = Context.Parse(Console.In, previousCtx, map);
            stopWatch.Restart();

            if(ctx.Turn == 0) {
                MY_SIDE = ctx.Cells.Where(c => c.Owner == ME).First().Id % 2; 
                Console.Error.WriteLine("I'm on " + (MY_SIDE != 0 ? "LEFT" : "RIGHT"));            
            }
            
            Console.Error.WriteLine("----[ {0:D3} ]  vs   [ {1:D3} ]---", ctx.MyScore, ctx.EnemyScore);
            var myCells = ctx.Cells.Where(c => c.Owner == ME).OrderBy(c => -c.Troups).ToList();
            var othCells = ctx.Cells.Where(c => c.Owner != ME).ToList();
            
            // --------------- INITIALIZE ACTIONS FOR THIS TURN ----------------------------------------
            var actions = new Dictionary<int, List<Action>>();            
            foreach(var myC in myCells) {
                actions.Add(myC.Id, new List<Action>());                
            }
            
            // --------------- LOOK FOR CELLS TO DEFEND/ATTACK ----------------------------------------
            foreach(var myC in myCells) {
                int totalIncoming = 0;
                int troupsLeft = myC.Troups; int reserve = myC.Capacity;
                int futureTroups = troupsLeft;
                
                if(myC.Incoming.Count > 0) {
                    var underAttack = myC.Incoming;
                    var firstWave = myC.Incoming.FirstOrDefault();
                    
                    totalIncoming = myC.CountIncoming();
                    Console.Error.WriteLine(myC + " under attack in " + firstWave.Time + " w/ " + firstWave.Size + " (total incoming = " + totalIncoming + ")");
                               
                    int lastIncomingTime = 0; int lastReinforcementTime = 0;
                    var incomingIt = myC.Incoming.GetEnumerator(); var reinforcementsIt = myC.Reinforcements.GetEnumerator();
                    while(incomingIt.MoveNext()) {
                        if(lastIncomingTime < incomingIt.Current.Time) {
                            futureTroups += myC.Capacity;
                            lastIncomingTime = incomingIt.Current.Time;
                        }
                        futureTroups -= incomingIt.Current.Size;
                        if(futureTroups <= 0) break;
                    }
                    
                    if(futureTroups <= 0) {
                        Console.Error.WriteLine("I will loose factory " + myC.Id + " and " + " troups with it");
                    }
                }
                
                var interestingCells = othCells
                                        .Where(c => c.Troups < futureTroups && c.Capacity > 0)
                                        .OrderBy(c => -c.Capacity)
                                        .ThenBy(c => map.ShortestDist(myC.Id, c.Id))
                                        .ThenBy(c => c.Troups);
                     
                foreach(var ic in interestingCells) {                        
                    Console.Error.WriteLine("{0} troups = {1}, capacity = {2} -> {3}", myC, myC.Troups, myC.Capacity, ic);
                    int extra = ic.Owner == CPU ? ic.Capacity : 0;
                    int holdBack = myC.Capacity < ic.Capacity ? 0 : myC.Capacity - ic.Capacity;
                    if(ic.Troups + holdBack + extra < troupsLeft) {
                        var sp = map.ShortestPath(myC.Id, ic.Id);                    
                        actions[myC.Id].Add(Action.Move(myC.Id, sp[0].B, ic.Troups + 1 + extra));
                        futureTroups -= (ic.Troups+1);
                    }
                    if(futureTroups <= reserve)
                        break;
                }
                
                if(futureTroups > 10 && myC.Capacity < 3) {
                    actions[myC.Id].Add(Action.Inc(myC.Id));
                }
            }
            
            if(ctx.EnemyBombs.Count() > 0) {                
                Console.Error.WriteLine(" -----  INCOMING BOMBS ----- ");
                var defenseTarget = FindDefensiveTarget(null, ctx, map);
                
                if(defenseTarget != null) {                    
                    foreach(var myC in myCells) {
                        if(myC.Troups > 0) {
                            defenseTarget = FindDefensiveTarget(myC, ctx, map);
                            actions[myC.Id].Clear();
                            actions[myC.Id].Add(Action.Move(myC.Id, defenseTarget.Id, myC.Troups));
                        }
                    }
                }
            }

            int bombsLeft = ctx.MyBombsLeft;
            bool newBomb = ctx.EnemyBombs.Any(b => b.OriginalTime == b.Time); // there's at least one enemy bomb launched this turn!   
            if(newBomb) {    
                if(bombsLeft > 0) {
                    Console.Error.WriteLine("looing for a bomb stgy");
                    var bomberDest = othCells.Where(oc => oc.Owner == CPU && oc.Offline == 0
                                                        && (ctx.MyBombs.Count() == 0 || ctx.MyBombs.First().To != oc.Id))
                                             .OrderBy(oc => -oc.Capacity)
                                             .FirstOrDefault();
                    if(bomberDest != null) {
                        Console.Error.WriteLine(".. bmb dest = " + bomberDest);
                        var bomber = myCells
                                    //.Where(c => c.Troups == 0)
                                    .OrderBy(c => map.Dist(c.Id, bomberDest.Id))
                                    .FirstOrDefault();
                        if(bomber != null) {                            
                            Console.Error.WriteLine(".. bmber = " + bomber);
                            actions[bomber.Id].Clear();
                            actions[bomber.Id].Add(Action.Bomb(bomber.Id, bomberDest.Id));
                            bombsLeft--;                            
                        }
                    }
                }
            }
            
            
            if (bombsLeft > 0) {
                Cell firstBombSite = null;
                // if I have any bombs left, look through potential destinations
                firstBombSite = othCells
                                    .Where(c => c.Owner == CPU 
                                            && (c.Capacity == 3 || (c.Troups >= 10 && c.Capacity > 1))
                                            && c.Offline == 0
                                            && (myBombs.Count == 0 || myBombs[0].To != c.Id))
                                    .OrderBy(c => -c.Capacity).ThenBy(c => -c.Troups)
                                    .FirstOrDefault();
                                
                if(firstBombSite != null && (ctx.MyScore < ctx.EnemyScore - 1 || ctx.Turn > 15)) {
                    Console.Error.WriteLine(" [CONSIDER THE BOMB TO: " + firstBombSite + "]");
                    
                    var bombFrom = myCells.OrderBy(c => c.Capacity)
                                    .ThenBy(c => map.Dist(c.Id, firstBombSite.Id))
                                    .ThenBy(c => actions[c.Id].Count)
                                    .FirstOrDefault();
                    Console.Error.WriteLine("consider to bomb from " + bombFrom.Id);
                    actions[bombFrom.Id].Clear();
                    actions[bombFrom.Id].Add(Action.Bomb(bombFrom.Id,firstBombSite.Id));
                    bombsLeft--;
                    
                    Console.Error.WriteLine("First choice for bombing: {0}", firstBombSite);
                }                
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

    private static Cell FindDefensiveTarget(Cell fromCell, Context ctx, Map map) {
        // TODO:  rewrite this to find a destination where to move troops that is close, then bring them back when bomb explodes..        
        var defenseTarget = ctx.Cells.Where(oc => oc.Owner != ME && oc.Capacity >= 1)
                                    .OrderBy(oc => -oc.Capacity)                                    
                                    .ThenBy(oc => oc.Troups)
                                    .ThenBy(oc => ((oc.Id % 2) == MY_SIDE?0:1))
                                    // potentially leave this empty
                                    .ThenBy(oc => fromCell != null ? map.Dist(fromCell.Id, oc.Id) : -oc.Owner)
                                    .FirstOrDefault();
        if(defenseTarget == null) {
            defenseTarget = ctx.Cells.Where(oc => oc.Owner != ME && oc.Capacity > 0)
                            .OrderBy(oc => -oc.Capacity)
                            .ThenBy(oc => oc.Troups)
                            .ThenBy(oc => ((oc.Id % 2) == MY_SIDE?0:1))
                            // potentially leave this empty
                            .ThenBy(oc => fromCell != null ? map.Dist(fromCell.Id, oc.Id) : -oc.Owner)
                            .FirstOrDefault();
        }
        return defenseTarget;
    }
}
