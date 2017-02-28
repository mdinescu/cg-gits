using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{
    public static int ME = 1;
    public static int CPU = -1;
    public static string FACTORY = "FACTORY";
    public static string TROUP = "TROOP";
    public static string BOMB = "BOMB";
    
    private static int[][] NEXT;
    private static int[][] SP_D;
    
    class Link {
        public Link(Cell a, Cell b, int d) {
            A = a; B = b; Dist = d;
        }
        public int Dist { get; set; }
        public Cell A { get; set; }
        public Cell B { get; set; }
        
        public override string ToString() {
            return String.Format("[{0}:{1} ({2})]", A, B, Dist);
        }
    }
    class Cell {
        public Cell(int id) {
            Id = id;
            Links = new Dictionary<int, Link>();
        }
        public int Id { get;set; }
        public Dictionary<int, Link> Links { get; } 
        public int Owner { get; set; }
        public int Troups { get; set; }
        public int Capacity { get; set; }
        
        public Cell ClosestEnemy { get; set; }
        
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
        public Bomb(int id) {
            Id = id;
        }
        public int Id { get;set; }
        public int From { get; set; }
        public int To { get; set; }
        public int Time { get; set; }
        public int Owner { get; set; }
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
        public override String ToString() { return Verb + " " + From + " " + To + (Count > 0 ? (" " + Count) : ""); }
        
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
    
    static void ComputeShortestPaths(Dictionary<int, Cell> cells) {
        int N = cells.Count; int MXX = int.MaxValue;
        SP_D = new int[N][]; NEXT = new int[N][];
        for(int i = 0; i < N; i++) {
            SP_D[i] = new int[N]; NEXT[i] = new int[N];
            for(int j = 0; j < N; j++) {
                if(cells[i].Links.ContainsKey(j)) {
                    SP_D[i][j] = cells[i].Links[j].Dist;
                    NEXT[i][j] = j;
                }else {
                    SP_D[i][j] = MXX;    
                    NEXT[i][j] = -1;
                }                
                
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
    
    static int ShortestDist(int c1, int c2) {
        return SP_D[c1][c2];
    }
    static List<Link> ShortestPath(int c1, int c2, Dictionary<int, Cell> cells) {
        var path = new List<Link>();
        int cc = c1;
        while(cc != c2) {         
           int nc = NEXT[cc][c2];
           path.Add(cells[cc].Links[nc]);
           cc = nc;           
        }
        return path;
    }
    
    
    
    static void Main(string[] args)
    {
        string[] inputs;
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        
        
        int bombsLeft = 2;
        
        Dictionary<int, Cell> cells = new Dictionary<int, Cell>();
        for (int i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');            
            int factory1 = int.Parse(inputs[0]);
            if(!cells.ContainsKey(factory1))
                cells.Add(factory1, new Cell(factory1));
            int factory2 = int.Parse(inputs[1]);
            if(!cells.ContainsKey(factory2))
                cells.Add(factory2, new Cell(factory2));
            int dist = int.Parse(inputs[2]);
            var link1 = new Link(cells[factory1], cells[factory2], dist);
            var link2 = new Link(cells[factory2], cells[factory1], dist);
            cells[factory1].Links.Add(factory2, link1);
            cells[factory2].Links.Add(factory1, link2);    
        }
        
        ComputeShortestPaths(cells);
        Console.Error.WriteLine("-------------------------"); 
        
        List<Troup> myTroups = new List<Troup>();
        List<Troup> enemyTroups = new List<Troup>();
        var incomingT = new Dictionary<int, List<Troup>>();
        var outgoingT = new Dictionary<int, List<Troup>>();
        
        var myBombs = new List<Bomb>();
        var enemyBombs = new List<Bomb>();
        
        var newBomb = false;
        // game loop
        while (true)
        {
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            
            myTroups.Clear(); enemyTroups.Clear();
            incomingT.Clear(); outgoingT.Clear();
            
            newBomb = false; var allBombs = new List<Bomb>();
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]);
                string entityType = inputs[1];
                int arg1 = int.Parse(inputs[2]);
                int arg2 = int.Parse(inputs[3]);
                int arg3 = int.Parse(inputs[4]);
                int arg4 = int.Parse(inputs[5]);
                int arg5 = int.Parse(inputs[6]);                
                if(entityType == FACTORY) {
                    cells[entityId].Owner = arg1;
                    cells[entityId].Troups = arg2;
                    cells[entityId].Capacity = arg3;
                    //Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);                    
                }else if(entityType == TROUP) {
                    Troup t = new Troup(entityId);
                    t.Owner = arg1;
                    t.From = arg2;
                    t.To = arg3;
                    t.Size = arg4;
                    t.Time = arg5;
                    
                    if(t.Owner == ME) {
                        myTroups.Add(t);
                    }else {
                        enemyTroups.Add(t);
                    }
                    if(!incomingT.ContainsKey(t.To)) incomingT.Add(t.To, new List<Troup>()); 
                    incomingT[t.To].Add(t);
                }else {                
                    Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);
                    var bmb = new Bomb(entityId) { Owner = arg1, From = arg2, To = arg3, Time = arg4 };
                    allBombs.Add(bmb);
                    if (bmb.Owner == ME)
                        myBombs.Add(bmb);
                    else {
                        if(!enemyBombs.Any(b => b.Id == bmb.Id)) {
                            enemyBombs.Add(bmb);
                            newBomb = true;
                        }
                    }                    
                }
            }
            
            for(int i = myBombs.Count-1; i>=0; i--) {
                if(!allBombs.Any(b => b.Id == myBombs[i].Id)) {
                    myBombs.RemoveAt(i);
                }
            }
            for(int i = enemyBombs.Count-1; i>=0; i--) {
                if(!allBombs.Any(b => b.Id == enemyBombs[i].Id)) {
                    enemyBombs.RemoveAt(i);
                }
            }
            
            for(int i = 0; i < cells.Count; i++){
                var closestEnemy = cells.Values.Where(c => c.Owner != cells[i].Owner).OrderBy(c => ShortestDist(i, c.Id)).FirstOrDefault();
                cells[i].ClosestEnemy = closestEnemy;
            }
            
            
            Console.Error.WriteLine("-------------------------");            
            var myCells = cells.Values.Where(c => c.Owner == ME && c.Troups > 0).OrderBy(c => -c.Troups);
            var othCells = cells.Values.Where(c => c.Owner != ME);
            
            var actions = new Dictionary<int, List<Action>>();
            
            foreach(var myC in myCells) {                
                actions.Add(myC.Id, new List<Action>());                
            }
            
            foreach(var myC in myCells) {
                if(incomingT.ContainsKey(myC.Id)) {
                    var underAttack = incomingT[myC.Id].Where(t => t.Owner != ME).OrderBy(t => t.Time);
                    var firstWave = underAttack.FirstOrDefault();
                    if (firstWave != null) {
                        Console.Error.WriteLine(myC + " under attack in " + firstWave.Time + " w/ " + firstWave.Size + " (total incoming = " + underAttack.Sum(t => t.Size) + ")");
                    }
                }
                
                var interestingLinks = myC.Links.Values
                                        .Where(l => l.B.Owner != ME && l.B.Troups < myC.Troups && l.B.Capacity > 0)
                                        .OrderBy(l => -l.B.Capacity)
                                        .ThenBy(l => ShortestDist(myC.Id, l.B.Id))
                                        .ThenBy(l => l.B.Troups);
                
                int troupsLeft = myC.Troups; int reserve = myC.Capacity;
                     
                foreach(var il in interestingLinks) {                        
                    Console.Error.WriteLine("{0} troups = {1}, capacity = {2} -> {3}", myC, myC.Troups, myC.Capacity, il);
                    if(il.B.Troups + il.B.Capacity + reserve < troupsLeft) {
                        var sp = GetBestPathFwd(myC.Id, il.B.Id, cells);                    
                        
                        actions[myC.Id].Add(Action.Move(myC.Id, sp.Id, il.B.Troups + il.B.Capacity));
                        troupsLeft -= (il.B.Troups+1);
                    }
                    if(troupsLeft <= reserve)
                        break;
                }                
            }
            
            
            Cell firstBombSite = null;
            if(bombsLeft > 0) {
                // if I have any bombs left, look through potential destinations
                var firstChoice = othCells
                                    .Where(c => c.Owner == CPU && c.Troups >= 10)
                                    .OrderBy(c => -c.Capacity).ThenBy(c => -c.Troups)
                                    .FirstOrDefault();                
            }
            
            //var myInteriorCells = myCells.Where(c => c.Links.Values.All(l => l.B.Owner == ME));
            //foreach(var ic in myInteriorCells) {
            //    Console.Error.WriteLine("Interior cell: {0} has {1} troups", ic, ic.Troups);
            //}
            
            
            
            if(newBomb) {
                Console.Error.WriteLine(" -----  GOING FOR DEFENSE ----- ");            
                var defenseTarget = FindDefensiveTarget(othCells, null);
                
                if(defenseTarget != null) {                    
                    foreach(var myC in myCells) {                        
                        defenseTarget = FindDefensiveTarget(othCells, myC);
                        if(myC.Troups > 0) {
                            actions[myC.Id].Clear();
                            actions[myC.Id].Add(Action.Move(myC.Id, defenseTarget.Id, myC.Troups));
                        }
                    }
                }
                
                if(bombsLeft > 0 && newBomb) {
                    Console.Error.WriteLine("looing for a bomb stgy");
                    var bomberDest = othCells.Where(oc => oc.Owner == CPU).OrderBy(oc => -oc.Capacity).FirstOrDefault();
                    if(bomberDest != null) {
                        Console.Error.WriteLine(".. bmb dest = " + bomberDest);
                        var bomber = myCells
                                    //.Where(c => c.Troups == 0)
                                    .OrderBy(c => c.Links[bomberDest.Id].Dist)
                                    .FirstOrDefault();
                        if(bomber != null) {                            
                            Console.Error.WriteLine(".. bmber = " + bomber);
                            actions[bomber.Id].Clear();
                            actions[bomber.Id].Add(Action.Bomb(bomber.Id, bomberDest.Id));
                            
                            bombsLeft--;                            
                        }
                    }
                }
            }else {
                Console.Error.WriteLine(" -----  GOING FOR MOVES ----- ");                
                // TODO:  figure out if there are any more move for reinforcements etc.                
            }
            
            string msg = "(@)";
            if(firstBombSite != null) {
                    Console.Error.WriteLine("First choice for bombing: {0}", firstBombSite);
                    //var origin = myCells.Where(c => !tests.Any(t => t.Item1 == c.Id))
                     //               .FirstOrDefault();
                    //if(
                    
                    msg = "" + firstBombSite.Id;
            }
            
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
        }
    }
    
    
    private static Cell GetBestPathFwd(int from, int to, Dictionary<int, Cell> cells) {
        var sp = ShortestPath(from, to, cells); 
        if (sp[0].B.Owner == ME) return sp[0].B;
        if (sp.Count > 1) {            
            foreach(var pLink in sp) {
                if (pLink.B.Owner == ME) return pLink.B;
                if (pLink.B.Capacity == 0 && pLink.B.Troups < 2) return pLink.B;
                if (pLink.B.Capacity * ShortestDist(from, pLink.B.Id) > 3)
                    continue;                
            }   
        }
        return cells[to];
    }
    
    private static Cell FindDefensiveTarget(IEnumerable<Cell> cells, Cell myCell) {
        var defenseTarget = cells.Where(oc => oc.Capacity >= 2)
                                    .OrderBy(oc => -oc.Capacity)
                                    .ThenBy(oc => oc.Troups)
                                    .ThenBy(oc => myCell != null ? oc.Links[myCell.Id].Dist : -oc.Owner)
                                    .FirstOrDefault();
        if(defenseTarget == null) {
            defenseTarget = cells.Where(oc => oc.Capacity > 0)
                            .OrderBy(oc => -oc.Capacity)
                            .ThenBy(oc => oc.Troups)
                            .ThenBy(oc => myCell != null ? oc.Links[myCell.Id].Dist : -oc.Owner)
                            .FirstOrDefault();
        }
        return defenseTarget;
    }
}
