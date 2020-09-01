using System.Collections.Generic;
using System.Linq;

namespace InstructorScanner2.FunctionApp
{
    public static class CalendarDayListExtensions
    {
        public static List<string> GetDistinctIntials(this IList<CalendarDay> calendarDays)
        {
            return calendarDays
                .SelectMany(cd => cd.InstructorSlots)
                .Select(iSlots => iSlots.InstructorInitials)
                .Distinct()
                .ToList();
        }

        public static int CalculateTotalSlotCountForInstructor(this IList<CalendarDay> calendarDays, string instructorIntials, string slotAvailabilityName = AvailabilityNames.Free)
        {
            return calendarDays
                .SelectMany(cd => cd.InstructorSlots)
                .GroupBy(instructorSlots => instructorSlots.InstructorInitials)
                .Where(grp => grp.Key == instructorIntials)
                .Select(grp => grp.Sum(iSlots => iSlots.Slots.Where(s => s.Availability == slotAvailabilityName).ToList().Count))
                .Sum();
        }


        public static int CalculateTotalSlotCount(this IList<CalendarDay> calendarDays, string slotAvailabilityName = AvailabilityNames.Free)
        {
            return calendarDays
                .SelectMany(cd => cd.InstructorSlots)
                .Sum(iSlots => iSlots.Slots.Where(s => s.Availability == slotAvailabilityName).ToList().Count);
        }
    }
}
