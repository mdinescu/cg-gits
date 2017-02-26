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
        
        // game loop
        while (true)
        {
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
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
                Console.Error.WriteLine("E{0}.{1}: {2},{3},{4},{5}", entityType, entityId, arg1, arg2, arg3, arg4);
                if(entityType == FACTORY) {
                    cells[entityId].Owner = arg1;
                    cells[entityId].Troups = arg2;
                    cells[entityId].Capacity = arg3;
                }
            }
            
            for(int i = 0; i < cells.Count; i++){
                var closestEnemy = cells.Values.Where(c => c.Owner != cells[i].Owner).OrderBy(c => ShortestDist(i, c.Id)).FirstOrDefault();
                cells[i].ClosestEnemy = closestEnemy;
            }
            
            
            Console.Error.WriteLine("-------------------------");            
            var myCells = cells.Values.Where(c => c.Owner == ME && c.Troups > 0).OrderBy(c => -c.Troups);
            var othCells = cells.Values.Where(c => c.Owner != ME);
            
            var tests = new List<Tuple<int,int, int>>();
            
            
            foreach(var myC in myCells) {
                var interestingLinks = myC.Links.Values
                                        .Where(l => l.B.Owner != ME && l.B.Troups < myC.Troups)
                                        .OrderBy(l => -l.B.Capacity)
                                        .ThenBy(l => ShortestDist(myC.Id, l.B.Id))
                                        .ThenBy(l => l.B.Troups);
                var first = interestingLinks.FirstOrDefault();
                Console.Error.WriteLine("{0} troups = {1}, capacity = {2} -> {3}", myC, myC.Troups, myC.Capacity, first);
                if(first != null) {
                    var sp = ShortestPath(myC.Id, first.B.Id, cells);                    
                    tests.Add(Tuple.Create(myC.Id, sp[0].B.Id, first.B.Troups));
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
            
            Console.Error.WriteLine(" -----  GOING FOR MOVES ----- ");
            
            if(tests.Count > 0) {
                foreach(var mv in tests)
                    Console.Write("MOVE {0} {1} {2};", mv.Item1, mv.Item2, mv.Item3 + 1);
            }else {
                //var bestIC = myInteriorCells.OrderBy(ic => -ic.Troups).FirstOrDefault();
                //if(bestIC != null) {       
                //    var bestMove = bestIC.Links.Values.OrderBy(l => ShortestDist(l.A.Id, l.A.ClosestEnemy.Id));
                //}else {                
                    Console.Write("WAIT;");
                //}
            }
            
            string msg = "done";
            if(firstBombSite != null) {
                    Console.Error.WriteLine("First choice for bombing: {0}", firstBombSite);
                    //var origin = myCells.Where(c => !tests.Any(t => t.Item1 == c.Id))
                     //               .FirstOrDefault();
                    //if(
                    
                    msg = "" + firstBombSite.Id;
            }
            
            Console.WriteLine("MSG " + msg);
        }
    }
}
