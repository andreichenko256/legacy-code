namespace RepairService;

// Розрахунок вартості робіт
public class CostCalculator
{
    public const decimal DiagnosticFee = 200;
    public static bool UseOldTariff = false;   // TODO: прибрати після 2023

    // labor rate per device type, UAH
    public decimal LaborRate(string deviceType)
    {
        if (deviceType == "Phone") return 450;
        if (deviceType == "Tablet") return 550;
        if (deviceType == "Laptop" || deviceType == "PC") return 800;
        if (deviceType == "TV") return 900;
        if (deviceType == "Washer" || deviceType == "Fridge") return 1100; // з виїздом
        if (deviceType == "Console") return 600;
        // все інше - по мінімальній ставці
        return 400;
    }

    // кошторис для клієнта
    public decimal calcEst(string type, string priority, decimal parts)
    {
        if (UseOldTariff)
            return 350 + parts * 1.1m; // тариф 2021
        decimal k = 1;
        if (priority == "Express")
            k = 1.5m; // 1.5 - коефіцієнт терміновості
        decimal tmp = LaborRate(type) * k + parts;
        return Math.Round(tmp, 2);
    }
}
