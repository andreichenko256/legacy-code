namespace FitnessClub;

// Рецепція: вхід у зал і на заняття
public class VisitService {
    public static int PeopleInside = 0;
    public string LastMessage = "";
    private ClubRepository repo;
    private Member curMember;           // тільки під час CheckIn
    private Membership curMs;
    private int lastVisitNo = 1000;

    public VisitService(ClubRepository repository) {
        repo = repository;
    }

    // returns visit number (> 1000) or error code < 0, reason in LastMessage
    public int CheckIn(int memberId, int classId, bool staffOverride, string desk) {
        int res = 0;
        decimal fee = 0;
        curMember = repo.FindMember(memberId);
        if (curMember != null) {
            // 500 грн - ліміт боргу
            if (curMember.Debt <= 500 || staffOverride) {
                curMs = repo.GetLatestMembership(curMember.Id);
                if (curMs != null && curMs.Status != MembershipStatus.Cancelled) {
                    if (curMs.Status == MembershipStatus.Frozen) {
                        res = -3;
                        LastMessage = "Membership is frozen";
                    } else if (curMs.EndDate.Date <= DateTime.Today) {
                        // діє включно з останнім днем
                        res = -4;
                        LastMessage = "Membership expired";
                    } else if (curMs.Type == "Morning" && DateTime.Now.Hour >= 12) {
                        // ранковий - до 12:00
                        res = -6;
                        LastMessage = "Morning membership is valid till 12:00";
                    } else if (curMs.Type == "Visits10" && curMs.VisitsLeft < 0) {
                        res = -7;
                        LastMessage = "No visits left";
                    }
                } else {
                    res = -2;
                    LastMessage = "No valid membership";
                }
            } else {
                res = -5;
                LastMessage = "Debt " + curMember.Debt + " UAH, please pay at the desk";
            }
        } else {
            res = -1;
            LastMessage = "Member #" + memberId + " not found";
        }
        //if (PeopleInside >= 80)
        //    res = -11;
        if (res == 0 && classId > 0) {
            GroupClass gc = repo.FindClass(classId);
            if (gc == null) {
                res = -8;
                LastMessage = "Class #" + classId + " not found";
            } else {
                if (gc.Booked.Count < gc.Capacity) {
                    if (!gc.Booked.Contains(curMember.Id)) {
                        gc.Booked.Add(curMember.Id);
                        fee = ChargeClassFee(gc);
                    } else {
                        res = -10;
                        LastMessage = "Already booked for " + gc.Name;
                    }
                } else {
                    res = -9;
                    LastMessage = gc.Name + " is full (" + gc.Capacity + " places)";
                }
            }
        }
        if (res != 0) {
            AuditLog.Write("DENY member=" + memberId + " code=" + res);
            return res;
        }
        if (curMs.Type == "Visits10") {
            curMs.VisitsLeft = curMs.VisitsLeft - 1;
        }
        curMember.BonusPoints += 10;
        PeopleInside++;
        Visit v = new Visit();
        v.Id = ++lastVisitNo;
        v.MemberId = curMember.Id;
        v.ClassId = classId;
        v.Fee = fee;
        v.Desk = desk ?? "MAIN";
        repo.Visits.Add(v);
        AuditLog.Write("IN visit=" + v.Id + " member=" + v.MemberId + " desk=" + v.Desk);
        LastMessage = "Welcome, " + curMember.FirstName + "!";
        return v.Id;
    }

    // Standard has no classes included - the fee goes to member's tab
    private decimal ChargeClassFee(GroupClass gc) {
        decimal f = 0;
        if (curMs.Type == "Standard") {
            f = curMember.IsStudent ? 100 : 150;
            curMember.Debt += f;
            Console.WriteLine("  (" + gc.Name + ": " + f + " UAH to tab #" + curMember.Id + ")");
        }
        return f;
    }

    // TEMP: turnstile sends card code "FC-0007" until the firmware update
    public int CheckInByCard(string cardCode, int classId) {
        int id = 0;
        try {
            id = int.Parse(cardCode.Substring(3));
        } catch (Exception) {
        }
        return CheckIn(id, classId, false, "TURNSTILE");
    }

    // пікові години, з 2022 не використовується
    private bool IsPeakTime() {
        int h = DateTime.Now.Hour;
        return h >= 18 && h <= 21;
    }
}
