namespace PharmacyStock;

public class ReportPrinter
{
    private StockRepository repo;
    private decimal dayTotal;        // рахується в print_day

    public ReportPrinter(StockRepository r)
    {
        repo = r;
    }

    public void PrintReceipt(string no)
    {
        Prescription p = repo.FindPrescription(no);
        if (p == null)
            return;
        Medicine m = repo.FindMedicine(p.MedicineCode);
        Console.WriteLine("Receipt for " + p.PatientName + ", phone " + p.MaskedPhone());
        string line = p.Number + " | " + p.PatientName + " | " + p.MedicineCode;
        line = line + " | " + p.DispensedQty + "/" + p.Quantity;
        line = line + " | " + p.Status;
        if (p.Category != "")
            line = line + " | " + p.Category;
        if (p.Paid > 0)
            line = line + " | paid " + p.Paid.ToString("0.00");
        Console.WriteLine("  " + line);
        Console.WriteLine("  " + m.Name + ", " + p.Doctor + ", pharmacist " + p.LastPharmacist);
        if (p.HasBenefit())
            Console.WriteLine("  Benefit: " + p.Category.ToLower() + " (state program)");
        Console.WriteLine("  History: " + string.Join(" > ", p.History));
    }

    public void PrintStock()
    {
        Console.WriteLine("Usable stock:");
        foreach (Medicine m in repo.Medicines)
        {
            string code = m.Code;
            int total = 0;
            foreach (StockBatch b in repo.Batches)
            {
                if (b.MedicineCode != code)
                    continue;
                if (b.ExpiryDate < DateTime.Today)
                    continue;
                total = total + b.Qty;
            }
            string flag = total < 3 ? "  <-- reorder" : "";
            Console.WriteLine("  " + code.PadRight(7) + total.ToString().PadLeft(4) + flag);
        }
    }

    public void print_day()
    {
        dayTotal = 0m;
        int served = 0, open = 0, pens = 0, dis = 0;
        Console.WriteLine("Register:");
        foreach (Prescription p in repo.Prescriptions)
        {
            string line = p.Number + " | " + p.PatientName + " | " + p.MedicineCode;
            line = line + " | " + p.DispensedQty + "/" + p.Quantity;
            line = line + " | " + p.Status;
            if (p.Category != "")
                line = line + " | " + p.Category;
            if (p.Paid > 0)
                line = line + " | paid " + p.Paid.ToString("0.00");
            Console.WriteLine("  " + line);
            if (p.DispensedQty > 0)
                served++;
            dayTotal = dayTotal + p.Paid;
            switch (p.Category)
            {
                case "PENSIONER":
                    pens++;
                    break;
                case "DISABLED":
                    dis++;
                    break;
            }
            if (p.Status == PrescriptionStatus.Issued
                || p.Status == PrescriptionStatus.PartiallyDispensed)
                open++;
        }
        Console.WriteLine("Served: " + served + ", still open: " + open);
        Console.WriteLine("Benefit patients: " + pens + " pensioner(s), " + dis + " disabled");
        decimal vat = Math.Round(dayTotal * 7 / 107, 2);   // ПДВ на ліки 20%
        Console.WriteLine("Cash: " + dayTotal.ToString("0.00") + ", VAT " + vat.ToString("0.00"));
    }
}
