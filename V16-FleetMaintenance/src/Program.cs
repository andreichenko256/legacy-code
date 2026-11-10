namespace FleetMaintenance;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Fleet maintenance: weekly planning ===");
        FleetRepository repo = new FleetRepository();
        MaintenancePlanner planner = new MaintenancePlanner(repo);
        WorkOrderService ws = new WorkOrderService(repo);
        // водій скаржився на гальма ще в понеділок
        WorkOrder brakes = ws.Open("AA5521OP", "BRAKES", "DUE", 9520, 0, "driver complaint");
        ws.Start(brakes.Number, "Kravets");
        new Dashboard(repo).Print();
        Console.WriteLine("-- Plans --");
        List<PlanItem> truck = planner.Plan("AI4512KA", 350, false, true);
        planner.PrintPlan(truck);
        planner.PrintPlan(planner.Plan("AI7730KB", 350, true, false));
        planner.PrintPlan(planner.Plan("AA3318OP", 150, true, false));
        planner.PrintPlan(planner.Plan("AA5521OP", 120, false, true));
        List<PlanItem> octavia = planner.Plan("aa2045mx ", 100, false, true);
        planner.PrintPlan(octavia);
        planner.PrintPlan(planner.Plan("AA7001TT", 300, false, false));
        planner.PrintPlan(planner.Plan("AA1290EK", 100, false, false));
        planner.PrintPlan(planner.Plan("XX0000XX", 100, false, false));
        Console.WriteLine("-- Work orders --");
        Orders(repo, ws, truck[0].OrderNo, octavia[0].OrderNo);
        double sum = ws.Complete(brakes.Number, 9.5, 5400, 94350);
        Console.WriteLine("Complete " + brakes.Number + ": " + sum.ToString("F2"));
        WorkOrder insp = ws.Open("AI4512KA", "inspection", "DUE", 2475, 0, "OTK");
        sum = ws.Complete(insp.Number, 1.5, 0, 0);
        Console.WriteLine("Complete " + insp.Number + " (not started): " + sum.ToString("F2"));
        Console.WriteLine("-- Orders --");
        ws.PrintOrders();
        new Dashboard(repo).Print();
    }

    private static void Orders(FleetRepository repo, WorkOrderService ws, string no, string no2)
    {
        Console.WriteLine("Start " + no + ": " + ws.Start(no, "Bondarenko"));
        double sum = ws.Complete(no, 3, 4200, 412450);
        Console.WriteLine("Complete " + no + ": " + sum.ToString("F2"));
        try { ws.Start(no, "Bondarenko"); }
        catch (InvalidOperationException e) { Console.WriteLine("Start again: " + e.Message); }
        bool ok = ws.Postpone(no, 2, "no free bay");
        Console.WriteLine("Postpone " + no + ": " + ok + " (" + ws.lastMessage + ")");
        WorkOrder t = repo.findOrder(no2);
        ok = ws.Postpone(no2, 5, "tyres not delivered");
        Console.WriteLine("Postpone " + no2 + " by 5 days: " + ok + ", " + t.Status
            + ", in " + (t.PlannedDate - DateTime.Today).Days + " days");
        ok = ws.Postpone(no2, 0, "");
        Console.WriteLine("Postpone by 0 days: " + ok + " (" + ws.lastMessage + ")");
        Console.WriteLine("Start " + no2 + ": " + ws.Start(no2, "Kravets"));
        Console.WriteLine("Start WO-9999: " + ws.Start("WO-9999", "Kravets"));
        sum = ws.Complete(no2, 0, 0, 0);
        Console.WriteLine("Complete " + no2 + ", 0 h: " + sum + " (" + ws.lastMessage + ")");
    }
}
