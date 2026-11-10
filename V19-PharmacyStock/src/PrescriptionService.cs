namespace PharmacyStock;

public class PrescriptionService
{
    public static int DispenseCount;   // для звіту зміни
    public StockRepository repo;
    public string LastMessage = "";    // причина відмови

    public PrescriptionService(StockRepository r)
    {
        repo = r;
    }

    // days: 0 = стандартні 30 днів (контрольовані - 5)
    public string Issue(string patient, string category, string phone, string code, int qty,
        int days, string doctor)
    {
        LastMessage = "";
        if (string.IsNullOrEmpty(patient))
        {
            LastMessage = "Patient name is required";
            return null;
        }
        Medicine m = repo.FindMedicine(code);
        if (m == null)
        {
            LastMessage = "Unknown medicine " + code;
            return null;
        }
        if (qty <= 0 || qty > 10)
            throw new ArgumentException("Quantity must be 1..10, got " + qty);
        if (m.IsControlled)
            days = 10;
        else if (days <= 0)
            days = 30;
        Prescription p = new Prescription();
        p.Number = repo.NextNumber();   // TODO: номер з eHealth
        p.PatientName = patient;
        p.Category = category;
        p.Phone = phone;
        p.MedicineCode = code;
        p.Quantity = qty;
        p.IssuedOn = DateTime.Now;
        p.ValidDays = days;
        p.Doctor = doctor;
        p.History.Add("Issued");
        repo.Prescriptions.Add(p);
        // SmsGateway.Send(phone, "Rx " + p.Number + " registered");
        // p.History.Add("SMS");
        AuditLog.Write("ISSUE", p.Number + " " + code + " x" + qty);
        return p.Number;
    }

    // Відпуск за рецептом. Спочатку перевіряємо p:
    // чи рецепт є, чи не закритий і чи не минув строк.
    // Потім кількість: q - скільки просять, res2 -
    // залишок за рецептом (q = 0 - видати весь залишок).
    // Контрольовані: потрібен код провізора who, ліміт
    // MaxPerDispense за раз, без заміни, запис у журнал.
    // Не вистачає і x = true (згода на заміну) - беремо
    // дженерик з тією ж МНН; c - код того, що видаємо.
    // Списуємо партії (найближчий термін першим),
    // прострочені пропускаємо; n - скільки видали.
    // Видали менше, ніж треба - PartiallyDispensed.
    // k - пільга: пенсіонери 50%, інваліди безкоштовно.
    // Повертає суму; -1 - відмова (див. LastMessage).
    public decimal Dispense(string no, int q, bool x, string who)
    {
        DispenseCount++;
        LastMessage = "";
        // 1. рецепт
        Prescription p = repo.FindPrescription(no);
        if (p == null)
        {
            LastMessage = "Prescription " + no + " not found";
            return -1;
        }
        if (p.Status > PrescriptionStatus.PartiallyDispensed)   // вже закритий
        {
            LastMessage = "Prescription " + no + " is " + p.Status;
            return -1;
        }
        if ((DateTime.Today - p.IssuedOn.Date).Days > p.ValidDays)
        {
            p.Status = PrescriptionStatus.Expired;
            LastMessage = "Prescription " + no + " has expired";
            return -1;
        }
        Medicine m = repo.FindMedicine(p.MedicineCode);
        string c = m.Code;
        // 2. кількість
        int res2 = p.Quantity - p.DispensedQty;
        if (q <= 0 || q > res2)
            q = res2;
        if (m.IsControlled)
        {
            if (string.IsNullOrEmpty(who))
            {
                LastMessage = "Pharmacist code required for " + m.Name;
                return -1;
            }
            q = Math.Min(q, m.MaxPerDispense);
            x = false;
            Console.WriteLine("  [controlled log] " + m.Name + " x" + q + ", " + who);
        }
        // 3. заміна на дженерик
        if (x && StockOf(c) < q)
        {
            foreach (Medicine g in repo.Medicines)
            {
                if (g.Inn == m.Inn && g.Code != c)
                {
                    if (StockOf(g.Code) >= q)
                    {
                        p.History.Add("Generic " + g.Code);
                        c = g.Code;
                        break;
                    }
                }
            }
        }
        // 4. списання з партій (FEFO)
        int n = 0;
        foreach (StockBatch b in repo.BatchesFor(c))
        {
            if (b.ExpiryDate < DateTime.Today)
                continue;
            int t = Math.Min(b.Qty, q - n);
            b.Qty = b.Qty - t;
            n = n + t;
        }
        if (n == 0)
        {
            LastMessage = "Out of stock: " + m.Name;
            return 0;
        }
        // 5. статус рецепта
        p.DispensedQty = p.DispensedQty + n;
        p.LastPharmacist = who;
        if (p.DispensedQty >= p.Quantity)
            p.Status = PrescriptionStatus.Dispensed;
        else
            p.Status = PrescriptionStatus.PartiallyDispensed;
        p.History.Add(p.Status + " " + n);
        // 6. сума з урахуванням пільги
        decimal k = 1m;
        switch (p.Category)
        {
            case "PENSIONER":
                k = 0.5m;
                if (DateTime.Today < new DateTime(2025, 4, 1))
                    k = 0.7m;
                break;
            case "DISABLED":
                k = 0m;
                break;
        }
        decimal s = Math.Round(m.Price * n * k, 2);
        p.Paid = p.Paid + s;
        AuditLog.Write("DISPENSE", no + " " + c + " x" + n + " = " + s);
        return s;
    }

    public bool ChangeStatus(string no, PrescriptionStatus to)
    {
        Prescription p = repo.FindPrescription(no);
        if (p == null)
            return false;
        bool ok = false;
        if (p.Status == PrescriptionStatus.Issued)
            ok = to == PrescriptionStatus.Cancelled || to == PrescriptionStatus.Expired;
        else if (p.Status == PrescriptionStatus.PartiallyDispensed)
            ok = to > PrescriptionStatus.PartiallyDispensed;   // закрити залишок
        if (!ok)
        {
            Console.WriteLine("Cannot change " + no + ": " + p.Status + " -> " + to);
            return false;
        }
        p.Status = to;
        p.History.Add(to.ToString());
        AuditLog.Write("STATUS", no + " -> " + to);
        return true;
    }

    private int StockOf(string code)
    {
        int total = 0;
        foreach (StockBatch b in repo.Batches)
        {
            if (b.MedicineCode != code)
                continue;
            if (b.ExpiryDate < DateTime.Today)
                continue;
            total = total + b.Qty;
        }
        return total;
    }
}
