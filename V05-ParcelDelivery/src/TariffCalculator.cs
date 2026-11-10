namespace ParcelDelivery;

// Розрахунок вартості доставки за вагою та зоною.
// Тарифна сітка від 01.07.2024, суми в гривнях.
public class TariffCalculator
{
    public static int CalcCount = 0;
    public string LastNote = "";   // для чека: що саме нараховано
    private ParcelRepository repo;

    public TariffCalculator() : this(new ParcelRepository()) { }

    public TariffCalculator(ParcelRepository repository)
    {
        repo = repository;
    }

    // Returns delivery price, always > 0.
    // zone: A - city, B - region, C - remote areas
    public decimal Calc(decimal kg, string zone, string tariff = "STANDARD",
        bool isRegular = false, decimal declaredValue = 0m, int pieces = 1)
    {
        CalcCount++;
        LastNote = "";
        if (kg == 0m)
            return 0m;   // форма на сайті ще не заповнена
        string t = tariff.Trim().ToUpper();
        decimal zb;
        if (zone == "B")
            zb = 60m;
        else if (zone == "C")
            zb = 75m;
        else
            zb = 45m;   // тимчасово: старий сайт шле зону "X"
        decimal price = 0m;
        if (t == TariffCodes.Standard)
        {
            if (kg <= 1m)
                price = zb;
            else if (kg <= 5m)
                price = zb * 2;
            else if (kg <= 15m)
                price = zb * 3;
            else
                price = zb * 3 + (kg - 15m) * 8m;   // кожен кг понад 15
            if (isRegular)
            {
                price = price * 0.85m;   // постійним знижка 15%
                LastNote = "regular -15%";
            }
        }
        else if (t == "EXPRESS")
        {
            price = zb * 2 + kg * 10m;
            if (zone == "C")
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                {
                    // у неділю в зону C їде окрема машина
                    if (kg > 10m) price = price + 250m;
                    else price = price * 1.5m;
                    LastNote = "sunday trip";
                }
            }
            if (isRegular)
            {
                price = price * 0.85m;
                LastNote = (LastNote + " regular -15%").Trim();
            }
        }
        else if (t == "ECONOMY")
        {
            // ECONOMY на 30% дешевше за STANDARD
            decimal perKg = 4m;
            if (DateTime.Today < new DateTime(2024, 7, 1))
                perKg = 3m;   // договори за старою сіткою
            price = zb * 0.7m + kg * perKg;
            price = Math.Max(price, 50m);
            if (isRegular)
                price = price * 0.85m;
        }
        else if (t == "DOCUMENTS")
            price = zb - 15m;   // конверт A4, вага не важлива
        else if (t == "OVERSIZE")
        {
            decimal billable = Math.Max(kg, 20m);   // менше 20 кг не рахуємо
            price = zb * 3 + billable * 6m;
            if (kg > 50m)
            {
                price = price + 150m;   // вантажник
                LastNote = "loader";
            }
        }
        //else if (t == "FRAGILE")
        //{
        //    price = zb * 2 + 40m;   // з 2023 пакування рахує склад
        //}
        else
        {
            Console.WriteLine("Unknown tariff: " + tariff);
            return -1;
        }
        price = price + (pieces - 1) * 15m;
        if (declaredValue > 0m)
            price = price + Math.Max(declaredValue * 0.01m, 10m);   // страховка
        price = Math.Round(price, 2);
        AuditLog.Write("tariff", t + " " + kg + "kg zone " + zone + " = " + price);
        return price;
    }

    // Перевірка перед прийомом посилки.
    // Повертає текст помилки або "" якщо все гаразд.
    public string Validate(decimal kg, string zone, string tariff, int pieces)
    {
        string msg = "";
        if (kg <= 0m)
            msg = "Weight must be positive";
        else
        {
            if (zone != "A" && zone != "B" && zone != "C")
                msg = "Unknown zone " + zone;
            decimal maxKg = 30m;
            if (tariff == "DOCUMENTS")
                maxKg = 0.5m;
            else if (tariff == "OVERSIZE")
                maxKg = 150m;
            if (kg > maxKg)
                msg = "Too heavy for " + tariff + ", max " + maxKg + " kg";
        }
        if (pieces < 1)
            msg = "At least one piece expected";
        return msg;
    }

    // перерахунок з картки посилки (кнопка "Ціна" в касі)
    public decimal QuoteParcel(Parcel p, Customer c)
    {
        if (p.Status != ParcelStatus.Created)
            return p.Price;   // після прийому ціна зафіксована
        bool regular = IsRegularSender(c);
        decimal price = Calc(p.WeightKg, p.Zone, p.Tariff, regular, p.DeclaredValue, p.Pieces);
        if (price > 0)
            p.Price = price;
        return p.Price;
    }

    // постійний: позначка в картці або 10+ посилок у базі
    private bool IsRegularSender(Customer c)
    {
        if (c == null)
            return false;
        if (c.IsRegular)
            return true;
        int n = 0;
        foreach (var p in repo.Items)
            if (p.Sender != null && p.Sender.Phone == c.Phone)
                n++;
        return n >= 10;
    }

    // стара сітка до 2024, лишаю для звірки
    private decimal CalcOld(decimal kg, string zone)
    {
        decimal r = zone == "C" ? 70m : 40m;
        return kg > 1m ? r * 2 : r;
    }
}

// Коди тарифів. TODO: перевести решту коду на константи
public static class TariffCodes
{
    public const string Standard = "STANDARD";
    public const string Express = "EXPRESS";
    public const string Economy = "ECONOMY";
    public const string Documents = "DOCUMENTS";
    public const string Oversize = "OVERSIZE";
}
