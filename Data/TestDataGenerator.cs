using System;
using System.Collections.Generic;

namespace Apiary.Data
{
    /// <summary>
    /// Static helper class providing realistic Australian test data for seeding
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random Random = new();

        // Australian first names
        private static readonly string[] FirstNames =
        [
            "Oliver", "Jack", "William", "Noah", "James", "Lucas", "Henry", "Ethan", "Leo", "Mason",
            "Charlotte", "Olivia", "Amelia", "Isla", "Mia", "Ava", "Grace", "Chloe", "Ella", "Sophie",
            "Liam", "Thomas", "Charlie", "Oscar", "Harry", "George", "Max", "Alexander", "Benjamin", "Samuel",
            "Emily", "Harper", "Zoe", "Willow", "Lily", "Ruby", "Ivy", "Aria", "Evelyn", "Layla"
        ];

        private static readonly string[] LastNames =
        [
            "Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Johnson", "White", "Martin", "Anderson",
            "Thompson", "Nguyen", "Thomas", "Walker", "Harris", "Lee", "Ryan", "Robinson", "Kelly", "King",
            "Chen", "Davis", "Wright", "Evans", "Roberts", "Green", "Hall", "Wood", "Jackson", "Clarke",
            "Patel", "Khan", "Mitchell", "Campbell", "Edwards", "Murphy", "Collins", "Singh", "Morris", "Hughes"
        ];

        private static readonly string[] StreetNames =
        [
            "High Street", "Church Street", "Station Road", "Victoria Street", "Park Road", "Main Street",
            "George Street", "King Street", "Queen Street", "Albert Street", "William Street", "Elizabeth Street",
            "Collins Street", "Bourke Street", "Flinders Street", "Pitt Street", "Bridge Road", "Bay Street",
            "Beach Road", "Chapel Street", "Commercial Road", "Princes Highway", "Pacific Highway", "Great Western Highway"
        ];

        private static readonly string[] Suburbs =
        [
            "Richmond", "Carlton", "Fitzroy", "South Yarra", "Prahran", "St Kilda", "Brighton", "Hawthorn",
            "Camberwell", "Malvern", "Toorak", "Kew", "Balwyn", "Box Hill", "Glen Waverley", "Doncaster",
            "Northcote", "Brunswick", "Coburg", "Preston", "Thornbury", "Essendon", "Moonee Ponds", "Footscray",
            "Parramatta", "Chatswood", "Bondi", "Manly", "Surry Hills", "Newtown", "Marrickville", "Redfern"
        ];

        private static readonly string[] States = ["VIC", "NSW", "QLD", "WA", "SA", "TAS", "ACT", "NT"];

        private static readonly string[] Industries =
        [
            "Retail", "Healthcare", "Construction", "Information Technology", "Education", "Hospitality",
            "Manufacturing", "Finance", "Professional Services", "Transport & Logistics", "Agriculture",
            "Arts & Entertainment", "Real Estate", "Automotive", "Food & Beverage", "Media & Communications"
        ];

        private static readonly string[] CompanyPrefixes =
        [
            "Australian", "National", "Metro", "Premier", "United", "Pacific", "Southern", "Northern",
            "Eastern", "Western", "Central", "Capital", "Elite", "Prime", "Quality", "Precision"
        ];

        private static readonly string[] CompanySuffixes =
        [
            "Solutions", "Services", "Industries", "Group", "Corporation", "Enterprises", "Company",
            "Partners", "Associates", "Holdings", "Consulting", "Systems", "Technologies", "Works"
        ];

        private static readonly string[] JobTitles =
        [
            "Manager", "Supervisor", "Coordinator", "Director", "Team Leader", "Assistant Manager",
            "Operations Manager", "Store Manager", "Site Supervisor", "Shift Supervisor", "Office Manager"
        ];

        private static readonly string[] PlacementRoles =
        [
            "Retail Assistant", "Office Admin", "Customer Service", "Warehouse Assistant", "Kitchen Hand",
            "Childcare Assistant", "Veterinary Assistant", "Library Assistant", "IT Support", "Marketing Assistant",
            "Accounting Assistant", "Reception", "Data Entry", "Stock Control", "General Hand"
        ];

        private static readonly string[] LogbookTaskDescriptions =
        [
            "Assisted customers with product inquiries and provided recommendations",
            "Processed incoming stock deliveries and updated inventory system",
            "Operated point of sale system and handled cash transactions",
            "Maintained cleanliness and organization of work area",
            "Filed documents and organized paperwork",
            "Answered phone calls and directed to appropriate staff",
            "Prepared and served food items following hygiene standards",
            "Assisted with data entry and spreadsheet management",
            "Helped set up displays and promotional materials",
            "Observed and assisted with client meetings",
            "Sorted and distributed incoming mail and packages",
            "Helped prepare materials for upcoming projects",
            "Shadowed senior staff to learn procedures",
            "Completed online training modules",
            "Assisted with stocktake and inventory counts",
            "Greeted visitors and managed sign-in procedures",
            "Helped organize storage areas and shelving",
            "Participated in team meeting and took notes",
            "Learned to use workplace software and systems",
            "Assisted with basic administrative tasks"
        ];

        private static readonly string[] MedicalConditionDetails =
        [
            "Uses inhaler as needed, usually well controlled",
            "Carries EpiPen for severe allergic reactions to nuts",
            "Type 1 diabetes, manages with insulin pump",
            "Mild asthma, triggered by exercise",
            "Allergic to penicillin medications",
            "Takes daily medication for ADHD",
            "Has mild epilepsy, well controlled with medication",
            "Severe peanut allergy, requires careful monitoring",
            "Seasonal hay fever, takes antihistamines",
            "Lactose intolerant, manages with diet"
        ];

        private static readonly string[] Relationships = ["Parent", "Mother", "Father", "Guardian", "Grandparent", "Uncle", "Aunt", "Sibling"];

        public static string GetFirstName() => FirstNames[Random.Next(FirstNames.Length)];
        public static string GetLastName() => LastNames[Random.Next(LastNames.Length)];
        public static string GetFullName() => $"{GetFirstName()} {GetLastName()}";

        public static string GetStreetAddress() => $"{Random.Next(1, 500)} {StreetNames[Random.Next(StreetNames.Length)]}";
        public static string GetSuburb() => Suburbs[Random.Next(Suburbs.Length)];
        public static string GetState() => States[Random.Next(States.Length)];
        public static string GetPostalCode() => Random.Next(2000, 9999).ToString();

        public static string GetAustralianPhone() => $"04{Random.Next(10000000, 99999999)}";
        public static string GetLandlinePhone() => $"0{Random.Next(2, 9)} {Random.Next(1000, 9999)} {Random.Next(1000, 9999)}";
        public static string GetEmail(string firstName, string lastName) => $"{firstName.ToLower()}.{lastName.ToLower()}@example.com";

        public static string GetIndustry() => Industries[Random.Next(Industries.Length)];
        public static string GetJobTitle() => JobTitles[Random.Next(JobTitles.Length)];
        public static string GetPlacementRole() => PlacementRoles[Random.Next(PlacementRoles.Length)];
        public static string GetRelationship() => Relationships[Random.Next(Relationships.Length)];

        public static string GetCompanyName()
        {
            var usePrefix = Random.Next(2) == 0;
            var industry = Industries[Random.Next(Industries.Length)].Split(' ')[0];

            if (usePrefix)
            {
                return $"{CompanyPrefixes[Random.Next(CompanyPrefixes.Length)]} {industry} {CompanySuffixes[Random.Next(CompanySuffixes.Length)]}";
            }
            return $"{GetLastName()} {industry} {CompanySuffixes[Random.Next(CompanySuffixes.Length)]}";
        }

        public static string GetLogbookTaskDescription() => LogbookTaskDescriptions[Random.Next(LogbookTaskDescriptions.Length)];
        public static string GetMedicalConditionDetails() => MedicalConditionDetails[Random.Next(MedicalConditionDetails.Length)];

        public static string GetStudentNumber(int index) => $"STU{DateTime.Now.Year}{index:D4}";
        public static string GetMedicareNumber() => $"{Random.Next(2000, 9999)} {Random.Next(10000, 99999)} {Random.Next(1, 9)}";

        public static DateOnly GetBirthDate(int minAge = 15, int maxAge = 18)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = Random.Next(minAge, maxAge + 1);
            return today.AddYears(-age).AddDays(-Random.Next(0, 365));
        }

        public static DateOnly GetPlacementStartDate()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            // Placements can be past, current, or future
            var daysOffset = Random.Next(-60, 30);
            return today.AddDays(daysOffset);
        }

        public static string GetWorkTime(bool isStart)
        {
            if (isStart)
            {
                var hours = new[] { "08:00", "08:30", "09:00", "09:30" };
                return hours[Random.Next(hours.Length)];
            }
            else
            {
                var hours = new[] { "16:00", "16:30", "17:00", "17:30" };
                return hours[Random.Next(hours.Length)];
            }
        }

        public static (string start, string lunchStart, string lunchEnd, string finish, decimal hours) GetLogbookTimes()
        {
            var startHour = Random.Next(8, 10);
            var startMinute = Random.Next(2) == 0 ? 0 : 30;
            var finishHour = Random.Next(16, 18);
            var finishMinute = Random.Next(2) == 0 ? 0 : 30;

            var lunchStartHour = 12;
            var lunchDuration = Random.Next(2) == 0 ? 30 : 60;

            var start = $"{startHour:D2}:{startMinute:D2}";
            var finish = $"{finishHour:D2}:{finishMinute:D2}";
            var lunchStart = $"{lunchStartHour:D2}:00";
            var lunchEnd = lunchDuration == 30 ? $"{lunchStartHour:D2}:30" : "13:00";

            var totalMinutes = (finishHour * 60 + finishMinute) - (startHour * 60 + startMinute) - lunchDuration;
            var hours = Math.Round(totalMinutes / 60.0m, 2);

            return (start, lunchStart, lunchEnd, finish, hours);
        }

        public static string GenerateToken()
        {
            // Generate a URL-safe random token
            var bytes = new byte[24];
            Random.NextBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "x")
                .Replace("/", "y")
                .Replace("=", "")[..32];
        }

        public static T GetRandom<T>(IList<T> list) => list[Random.Next(list.Count)];
        public static bool GetRandomBool(double trueChance = 0.5) => Random.NextDouble() < trueChance;
        public static int GetRandomInt(int min, int max) => Random.Next(min, max + 1);
    }
}
