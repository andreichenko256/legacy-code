namespace CafeOrders;

public class CafeRepository
{
    public static int nextId = 1;   // спільний для всіх кас
    public List<MenuItem> Menu = new List<MenuItem>();
    public List<Order> Orders = new List<Order>();

    public CafeRepository()
    {
        // з old POS: код;назва;ціна;категорія
        string[] rows = {
            "ESP;Espresso;45;DRINK", "AMR;Americano;50;DRINK", "CAP;Cappuccino;65;DRINK",
            "LAT;Latte;70;DRINK", "TEA;Black tea;40;DRINK", "LEM;Lemonade;75;DRINK",
            "MAT;Matcha latte;85;DRINK", "CRO;Croissant;55;BAKERY", "QUI;Quiche;120;MAIN",
            "CIN;Cinnamon bun;60;BAKERY", "SND;Club sandwich;145;MAIN",
            "BGL;Salmon bagel;165;MAIN", "CES;Caesar salad;175;MAIN", "BOR;Borscht;95;SOUP",
            "GRK;Greek salad;150;MAIN", "PEA;Pea soup;85;SOUP", "CHK;Chicken soup;90;SOUP",
            "MUS;Mushroom soup;105;SOUP", "CHC;Cheesecake;110;DESSERT",
            "TIR;Tiramisu;125;DESSERT"
        };
        foreach (string r in rows)
        {
            string[] f = r.Split(';');
            Menu.Add(new MenuItem { Code = f[0], Name = f[1], Price = decimal.Parse(f[2]),
                Category = f[3] });
        }
        Menu[6].Available = false;  // тимчасово: матчі нема з березня
    }

    public MenuItem findItem(string code)
    {
        foreach (MenuItem m in Menu)
        {
            if (m.Code == code)
                return m;
        }
        return null;
    }

    public Order FindOrder(int id)
    {
        foreach (Order o in Orders)
        {
            if (o.Id == id)
                return o;
        }
        return null;
    }
}

// till audit trail, file goes to the temp folder
public class AuditLog
{
    public static string FilePath = Path.Combine(Path.GetTempPath(), "cafe_audit.log");

    public static void Write(string msg)
    {
        try
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(FilePath, stamp + " " + msg + "\n");
        }
        catch { }
    }
}
