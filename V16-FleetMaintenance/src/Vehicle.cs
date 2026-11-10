namespace FleetMaintenance;

// Машина парку. Тип: TRUCK, VAN, CAR (у 2024 додали MINIBUS)
public class Vehicle
{
    public string Number;               // держномер без пробілів, AA1234BB
    public string Type;
    public string Status = "ACTIVE";    // ACTIVE / WORKSHOP / SOLD
    public string Make;
    public string Model;
    public int Year;
    public int Mileage;                 // км, з останньої путівки
    public string Tyres = "ALLSEASON";  // SUMMER / WINTER / ALLSEASON
    public DateTime Purchased;
    public string Driver;
    public string DriverPhone;          // як записали: 067..., +380...
    public string Depot = "Kyiv-1";
    public double FuelNorm;             // л/100 км, для бухгалтерії
}

// Запис сервісної книжки
public class ServiceRecord
{
    public string VehicleNo;
    public string Kind;                 // OIL, BRAKES, TYRES, INSPECTION
    public DateTime Date;
    public int Mileage;
    public double Cost;
}
