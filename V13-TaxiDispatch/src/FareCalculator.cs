namespace TaxiDispatch;

// Тарифи за наказом від 01.03.2021 (грн)
public class FareCalculator
{
    // вартість після завершення поїздки
    public double Calc(Ride r)
    {
        string cls = r.CarClass;
        double dx = r.DropX - r.PickupX, dy = r.DropY - r.PickupY;
        double km = Math.Sqrt(dx * dx + dy * dy) * 1.3;    // дороги не прямі
        double perKm = 9.0;
        if (cls == "COMFORT")
            perKm = 12.0;
        else if (cls == "BUSINESS")
            perKm = 18.5;
        else if (cls == "VAN")
            perKm = 14.0;
        double fare = 40 + km * perKm;
        if (fare < 70)
            fare = 70;
        fare = fare * r.Surge;
        if (r.ChildSeat)
            fare = fare + 30;
        // очікування: 3 хв безкоштовно, далі 3 грн/хв
        int waitMin = (int)(r.StartedAt - r.AssignedAt).TotalMinutes;
        if (waitMin > 3)
            fare = fare + (waitMin - 3) * 3;
        return Math.Round(fare);
    }

    // попередня ціна (без очікування і крісла)
    public double Estimate(string cls, double km)
    {
        double perKm = 9.0;
        if (cls == "COMFORT")
            perKm = 12.0;
        else if (cls == "BUSINESS")
            perKm = 18.5;
        else if (cls == "VAN")
            perKm = 14.0;
        double fare = 40 + km * perKm;
        if (fare < 70)
            fare = 70;
        return Math.Round(fare);
    }

    // штраф за скасування після призначення
    public double CancelFee(Ride r)
    {
        if (r.Status != RideStatus.Assigned)
            return 0;
        if (r.CarClass == "BUSINESS" || r.CarClass == "VAN")
            return 80;
        return 40;
    }

    // комісія сервісу 20%, для рейтингу 4.9+ - 15%
    public double DriverPayout(Driver d, double fare)
    {
        double commission = 0.2;
        if (d.Rating >= 4.9 && d.TripsToday >= 10)
            commission = 0.15;
        if (d.CarClass == "BUSINESS")
            commission = commission + 0.05;   // оренда авто
        return Math.Round(fare * (1 - commission), 2);
    }
}
