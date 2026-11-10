namespace ParcelDelivery;

// Каса відділення: прийом, статуси, чеки, зміна.
public class DeliveryService
{
    public ParcelRepository Repo;
    public string LastError = "";   // причина останньої відмови
    private TariffCalculator calc;

    public DeliveryService(ParcelRepository repo)
    {
        Repo = repo;
        calc = new TariffCalculator(repo);
    }

    // returns tracking number, or null (reason is in LastError)
    public string RegisterParcel(Customer sender, string recipient, string phone, string city,
        string zone, decimal kg, string tariff, decimal declared)
    {
        LastError = "";
        string err = calc.Validate(kg, zone, tariff, 1);
        if (err != "")
        {
            LastError = err;
            return null;
        }
        Parcel p = new Parcel();
        p.Sender = sender;
        p.RecipientName = recipient;
        p.RecipientPhone = phone;
        p.City = city;
        p.Zone = zone;
        p.Tariff = tariff;
        p.WeightKg = kg;
        p.DeclaredValue = declared;
        p.StatusChangedAt = DateTime.Now;
        calc.QuoteParcel(p, sender);
        if (p.Price <= 0)
        {
            LastError = "No price for tariff " + tariff;
            return null;
        }
        Repo.Add(p);
        sender.ParcelsSent++;
        p.History.Add(("Created " + calc.LastNote).Trim());
        AuditLog.Write("desk", p.TrackingNo + " created, price " + p.Price);
        return p.TrackingNo;
    }

    // Created -> Accepted -> InTransit -> Delivered.
    // Returned - з дороги або 14 днів після вручення.
    public bool ChangeStatus(string trackingNo, ParcelStatus to)
    {
        Parcel p = Repo.FindByTracking(trackingNo);
        if (p == null)
        {
            Console.WriteLine("Parcel " + trackingNo + " not found");
            return false;
        }
        bool ok = false;
        switch (p.Status)
        {
            case ParcelStatus.Created:
                ok = to == ParcelStatus.Accepted;
                break;
            case ParcelStatus.Accepted:
                ok = to == ParcelStatus.InTransit;
                break;
            case ParcelStatus.InTransit:
                ok = to == ParcelStatus.Delivered || to == ParcelStatus.Returned;
                break;
            case ParcelStatus.Delivered:
                // StatusChangedAt тут = дата вручення (статус Delivered)
                ok = to == ParcelStatus.Returned
                    && (DateTime.Today - p.StatusChangedAt.Date).Days <= 14;
                break;
        }
        if (!ok)
        {
            Console.WriteLine("Cannot change " + trackingNo + ": " + p.Status + " -> " + to);
            return false;
        }
        p.Status = to;
        p.StatusChangedAt = DateTime.Now;
        p.History.Add(to.ToString());
        AuditLog.Write("desk", trackingNo + " -> " + to);
        return true;
    }

    public void PrintReceipt(string trackingNo)
    {
        Parcel p = Repo.FindByTracking(trackingNo);
        if (p == null)
            return;
        Console.WriteLine("Receipt for " + p.Sender.Name + ", phone " + p.Sender.MaskedPhone());
        string line = p.TrackingNo + " | " + p.Zone + " | " + p.Tariff;
        line = line + " | " + p.WeightKg.ToString("0.00") + " kg";
        line = line + " | " + p.Status;
        if (p.Price > 0)
            line = line + " | " + p.Price.ToString("0.00");
        else
            line = line + " | n/a";
        Console.WriteLine("  " + line);
        Console.WriteLine("  History: " + string.Join(" > ", p.History));
        Console.WriteLine("  Delivery time: " + EstimateDays(p) + " day(s)");
    }

    // строки з договору з перевізником (2021)
    public int EstimateDays(Parcel p)
    {
        int d;
        switch (p.Tariff)
        {
            case "EXPRESS": d = 1; break;
            case "DOCUMENTS": d = 2; break;
            case "ECONOMY": d = 5; break;
            case "OVERSIZE": d = 4; break;
            default: d = 3; break;
        }
        if (p.Zone == "C")
            d = d + 2;
        return d;
    }

    // закриття зміни: кількість, повернення, виручка
    public void closeDay()
    {
        var items = Repo.Items;
        decimal total = 0m;
        int ret = 0;
        foreach (var p in items)
        {
            if (p.Status == ParcelStatus.Returned)
                ret++;
            else
                total = total + p.Price;
        }
        Console.WriteLine("Day closed: " + (items.Count - ret) + " parcel(s), "
            + ret + " returned");
        Console.WriteLine("Revenue: " + total.ToString("0.00") + " UAH, agent fee "
            + (total * 0.03m).ToString("0.00"));
    }
}
