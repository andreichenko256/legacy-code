namespace ShopOrders;

// In-memory storage. TODO: move to the database (planned for Q3 2021)
public class ShopRepository
{
    private static int _nextOrderId = 1001;
    public List<Product> Products = new List<Product>();
    public List<Customer> Customers = new List<Customer>();
    public List<Order> Orders = new List<Order>();
    public List<PromoCode> Promos = new List<PromoCode>();

    public void AddProduct(string sku, string name, decimal price, int stock, string category)
    {
        if (price <= 0)
            throw new ArgumentException("Invalid price for " + sku);
        Products.Add(new Product { Sku = sku.ToUpper(), Name = name, Price = price,
            Stock = stock, Category = category });
    }

    // повертає null, якщо додати не можна
    public Customer AddCustomer(int id, string name, string email, string phone,
        string level, DateTime registered)
    {
        if (email == null || email.IndexOf('@') < 1)
            return null;
        if (FindCustomer(id) != null)
            return null;
        Customer c = new Customer();
        c.Id = id;
        c.Name = name;
        c.Email = email.Trim().ToLower();
        c.Phone = phone;
        c.Level = level.ToUpper();
        c.RegisteredOn = registered;
        Customers.Add(c);
        return c;
    }

    public void AddPromo(string code, int percent, DateTime validUntil, int uses)
    {
        PromoCode pc = new PromoCode();
        pc.Code = code.ToUpper();
        pc.Percent = percent;
        pc.ValidUntil = validUntil;
        pc.UsesLeft = uses;
        Promos.Add(pc);
    }

    public Customer FindCustomer(int id)
    {
        foreach (Customer c in Customers)
            if (c.Id == id)
                return c;
        return null;
    }

    // HACK: SKU вводять як прийдеться, тому ToUpper
    public Product FindProduct(string sku)
    {
        if (sku == null) return null;
        string key = sku.Trim().ToUpper();
        for (int i = 0; i < Products.Count; i++)
            if (Products[i].Sku == key)
                return Products[i];
        return null;
    }

    public PromoCode findPromo(string code)
    {
        if (code == null || code.Trim() == "")
            return null;
        foreach (PromoCode pc in Promos)
        {
            if (pc.Code == code.Trim().ToUpper() && pc.UsesLeft > 0)
                return pc;
        }
        return null;
    }

    public Order GetOrder(int id)
    {
        Order found = null;
        foreach (Order o in Orders)
            if (o.Id == id) found = o;
        return found;
    }

    public void SaveOrder(Order o)
    {
        if (o.Id == 0)
            o.Id = _nextOrderId++;
        if (!Orders.Contains(o))
            Orders.Add(o);
    }
}
