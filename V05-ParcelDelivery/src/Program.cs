namespace ParcelDelivery;

// Demo run for the branch office (same scenario every morning)
public class Program
{
    public static void Main(string[] args)
    {
        var repo = new ParcelRepository();
        var calc = new TariffCalculator(repo);
        var service = new DeliveryService(repo);
        ShowTariffs(calc);
        string[] nos = RegisterParcels(service);
        Console.WriteLine("=== Status ===");
        Move(service, nos[0], ParcelStatus.Accepted, ParcelStatus.InTransit,
            ParcelStatus.Delivered, ParcelStatus.InTransit);
        Move(service, nos[1], ParcelStatus.Delivered, ParcelStatus.Accepted,
            ParcelStatus.InTransit, ParcelStatus.Returned);
        Move(service, nos[2], ParcelStatus.Accepted, ParcelStatus.InTransit,
            ParcelStatus.Delivered);
        // архівний запис: вручено 20 днів тому
        repo.FindByTracking(nos[2]).StatusChangedAt = DateTime.Today.AddDays(-20);
        Move(service, nos[2], ParcelStatus.Returned);
        Move(service, nos[0], ParcelStatus.Returned);
        Move(service, "PD999999", ParcelStatus.Accepted);
        Move(service, nos[3], ParcelStatus.Accepted);
        Console.WriteLine("=== Receipt ===");
        service.PrintReceipt(nos[0]);
        service.PrintReceipt("PD000404");
        Console.WriteLine("=== Couriers ===");
        var printer = new ManifestPrinter(repo);
        printer.PrintManifest("B");
        printer.PrintZoneSummary("B");
        Console.WriteLine("=== End of day ===");
        repo.PrintAll();
        service.closeDay();
        Console.WriteLine("Tariff calculations: " + TariffCalculator.CalcCount);
    }

    private static void ShowTariffs(TariffCalculator calc)
    {
        Console.WriteLine("=== Tariffs ===");
        Console.WriteLine("STANDARD 0.5 kg A: " + calc.Calc(0.5m, "A"));
        Console.WriteLine("STANDARD 5 kg A: " + calc.Calc(5.0m, "A"));
        Console.WriteLine("STANDARD 5 kg C: " + calc.Calc(5.0m, "C"));
        Console.WriteLine("STANDARD 0 kg A: " + calc.Calc(0.0m, "A"));
        Console.WriteLine("STANDARD 1 kg X: " + calc.Calc(1m, "X"));
        Console.WriteLine("STANDARD -2 kg A: " + calc.Calc(-2m, "A"));
        Console.WriteLine("STANDARD 2 kg B x3: " + calc.Calc(2m, "B", "STANDARD", false, 0m, 3));
        Console.WriteLine("EXPRESS 2 kg A: " + calc.Calc(2m, "A", "EXPRESS"));
        Console.WriteLine("ECONOMY 0.5 kg A: " + calc.Calc(0.5m, "A", "ECONOMY"));
        Console.WriteLine("ECONOMY 12 kg C regular: " + calc.Calc(12m, "C", "economy", true));
        Console.WriteLine("DOCUMENTS 0.3 kg C: " + calc.Calc(0.3m, "C", "DOCUMENTS"));
        Console.WriteLine("OVERSIZE 65 kg B: " + calc.Calc(65m, "B", "OVERSIZE"));
        Console.WriteLine("PALLET 10 kg A: " + calc.Calc(10m, "A", "PALLET"));
        try { calc.Calc(2m, "A", null); }
        catch (Exception ex) { Console.WriteLine("Calc without tariff: " + ex.GetType().Name); }
    }

    private static string[] RegisterParcels(DeliveryService service)
    {
        Console.WriteLine("=== Registration ===");
        var olena = new Customer("Olena Kovalenko", "+380501112233", true);
        var petro = new Customer("Petro Bondar", "0671234567", false);
        string t1 = service.RegisterParcel(olena, "Ivan Melnyk", "+380631231212", "Lviv",
            "A", 2.5m, "STANDARD", 0m);
        string t2 = service.RegisterParcel(petro, "Maria Shevchuk", "+380671112200", "Brody",
            "B", 4m, "EXPRESS", 1500m);
        string t3 = service.RegisterParcel(olena, "Taras Hnatiuk", "+380991234567", "Kosiv",
            "C", 7m, "ECONOMY", 0m);
        string t4 = service.RegisterParcel(petro, "Dim Mebliv", "+380322000000", "Stryi",
            "B", 70m, "OVERSIZE", 0m);
        Console.WriteLine("Registered: " + t1 + ", " + t2 + ", " + t3 + ", " + t4);
        TryRegister(service, petro, "A", -1m, "STANDARD");
        TryRegister(service, petro, "Q", 1m, "STANDARD");
        TryRegister(service, petro, "A", 1.2m, "DOCUMENTS");
        return new string[] { t1, t2, t3, t4 };
    }

    private static void TryRegister(DeliveryService s, Customer c, string zone, decimal kg,
        string tariff)
    {
        if (s.RegisterParcel(c, "Walk-in", "-", "Lviv", zone, kg, tariff, 0m) == null)
            Console.WriteLine("Rejected " + tariff + " " + kg + " kg: " + s.LastError);
    }

    private static void Move(DeliveryService s, string no, params ParcelStatus[] steps)
    {
        foreach (var to in steps)
            if (s.ChangeStatus(no, to))
                Console.WriteLine("  " + no + " is now " + to);
    }
}
