using System;
using System.Collections.Generic;
using System.Linq;

namespace InstructorScanner2.FunctionApp
{
    static public class CalendarDaysComparer
    {
        public static List<string> Compare(List<CalendarDay> oldValues, List<CalendarDay> newValues)
        {
            if (oldValues == null) throw new ArgumentNullException("oldValues");
            if (newValues == null) throw new ArgumentNullException("newValues");

            var instructorAvailability = new Dictionary<string, List<string>>();

            foreach(var newCalDay in newValues)
            {
                var oldCalDay = oldValues.SingleOrDefault(cd => cd.Date == newCalDay.Date)
                    ?? new CalendarDay { Date = newCalDay.Date, InstructorSlots = new List<InstructorSlots>() };

                foreach(var newInstructorSlots in newCalDay.InstructorSlots)
                {
                    if (newInstructorSlots.Slots.Any(s => s.Availability == AvailabilityNames.Free))
                    {
                        if (!instructorAvailability.ContainsKey(newInstructorSlots.InstructorInitials))
                        {
                            instructorAvailability.Add(newInstructorSlots.InstructorInitials, new List<string>());
                        }

                        var oldInstructorSlots = oldCalDay.InstructorSlots.SingleOrDefault(instructSlots => instructSlots.InstructorInitials == newInstructorSlots.InstructorInitials)
                            ?? new InstructorSlots { InstructorInitials = newInstructorSlots.InstructorInitials, Slots = new List<Slot>() };

                        var newAvailableSlots = FindNewAvailableSlots(oldInstructorSlots, newInstructorSlots);
                        if(newAvailableSlots.Count > 0)
                        {
                            var msgs =  instructorAvailability[newInstructorSlots.InstructorInitials];
                            msgs.Add(string.Empty);
                            msgs.Add($"<b>{newCalDay.Date: ddd dd-MMM}</b>");
                            msgs.AddRange(newAvailableSlots);
                        }

                    }
                }
            }

            var results = new List<string>();
            foreach(var initials in instructorAvailability.Keys)
            {
                results.Add(string.Empty);
                results.Add($"<b>Instructor: {initials}</b>");
                results.AddRange(instructorAvailability[initials]);
            }

            return results;
        }

        private static List<string> FindNewAvailableSlots(InstructorSlots oldInstructorSlots, InstructorSlots newInstructorSlots)
        {
            if (newInstructorSlots == null) throw new ArgumentNullException("newInstructorSlots");
            if (oldInstructorSlots == null)
            {
                oldInstructorSlots = new InstructorSlots { InstructorInitials = newInstructorSlots.InstructorInitials, Slots = new List<Slot>() };
            }

            if (oldInstructorSlots.InstructorInitials != newInstructorSlots.InstructorInitials) throw new InstructorScanException($"Old instructor slot initials '{oldInstructorSlots.InstructorInitials}' does not match new instructor slot initials '{newInstructorSlots.InstructorInitials}'");

            var messages = new List<string>();

            var newFeeSlots = newInstructorSlots.Slots.Where(s => s.Availability == AvailabilityNames.Free).ToList();
            foreach (var newFreeSlot in newFeeSlots)
            {
                var matchedOldLSlot = oldInstructorSlots.Slots.SingleOrDefault(s => s.Time == newFreeSlot.Time);
                if (matchedOldLSlot == null || (matchedOldLSlot.Availability != newFreeSlot.Availability))
                {
                    messages.Add($"{newFreeSlot.Time} is available");
                }
            }

            return messages;
        }
    }
}
