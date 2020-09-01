using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;
using System.Threading;

namespace InstructorScanner2.FunctionApp
{
    public interface IHtmlPageCreatorService
    {
        Task CreateHtmlPageAsync(List<CalendarDay> calendarDays, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class HtmlPageCreatorService : IHtmlPageCreatorService
    {
        private readonly IStorageHelper _storageHelper;

        public HtmlPageCreatorService(
            IStorageHelper storageHelper
        )
        {
            _storageHelper = storageHelper;
        }

        public async Task CreateHtmlPageAsync(List<CalendarDay> calendarDays, CancellationToken cancellationToken = default(CancellationToken))
        {
            var distinctSlots = calendarDays
                .SelectMany(cd => cd.InstructorSlots.SelectMany(instructSlots => instructSlots.Slots)
                .Select(s => s.Time))
                .Distinct()
                .ToList();

            var calendarDaysWithAvailableSlots = calendarDays
                .Where(cd => cd.InstructorSlots.Any(instructSlots => instructSlots.Slots.Any(s => s.Availability == AvailabilityNames.Free)))
                .ToList();

            var htmlPage = new StringBuilder();
            htmlPage.AppendLine("<!DOCTYPE html>");
            htmlPage.AppendLine("<html>");
            htmlPage.AppendLine("<head>");
            htmlPage.AppendLine("<style>");
            htmlPage.AppendLine("body { font-family: Arial, sans-serif; padding: 1rem; } table { font-size: 0.8rem; width: 100%; margin: 1.4rem auto; }");
            htmlPage.AppendLine("table,td,th { border-collapse: collapse;} th,td { padding: 0.5rem; border: solid 1px; } td { text-align: center; }");
            htmlPage.AppendLine(".bold { font-weight: bold; }");
            htmlPage.AppendLine("</style>");
            htmlPage.AppendLine("</head>");
            htmlPage.AppendLine("<body>");
            htmlPage.AppendLine("<h1>FI Available Slots</h1>");
            htmlPage.AppendLine($"<div><span class='bold'>Generated: </span><span>{DateTime.Now: dd-MMM-yyyy HH:mm:ss}</span></div>");
            htmlPage.AppendLine("<br>"); htmlPage.AppendLine("<br>");

            htmlPage.AppendLine("<table>");

            htmlPage.AppendLine("<thead>");
            htmlPage.AppendLine("<tr>");
            htmlPage.Append("<th>");
            htmlPage.Append("Date");
            htmlPage.Append("</th>");
            htmlPage.AppendLine();

            var idx = 0;
            var timeSlotIndex = new Dictionary<string, int>();
            foreach (var timeSlot in distinctSlots)
            {
                htmlPage.Append("<th>");
                htmlPage.Append(timeSlot);
                htmlPage.Append("</th>");

                timeSlotIndex.Add(timeSlot, idx);
                idx++;
            }

            htmlPage.AppendLine();
            htmlPage.AppendLine("</tr>");
            htmlPage.AppendLine("</thead>");

            htmlPage.AppendLine("<tbody>");


            foreach(var cd in calendarDaysWithAvailableSlots)
            {
                htmlPage.Append("<tr>");

                htmlPage.Append("<td>");
                htmlPage.Append($"{cd.Date: ddd}");
                htmlPage.Append("<br>");
                htmlPage.Append($"{cd.Date: dd-MMM}");
                htmlPage.Append("</td>");

                var slotRowData = distinctSlots.Select(ds => string.Empty).ToList();
                foreach (var instructorSlot in cd.InstructorSlots)
                {
                    var availableSlots = instructorSlot
                        .Slots
                        .Where(s => s.Availability == AvailabilityNames.Free)
                        .ToList();

                    foreach (var slot in availableSlots)
                    {
                        var slotIdx = timeSlotIndex[slot.Time];
                        var cellData = slotRowData[slotIdx];
                        slotRowData[slotIdx] = cellData.Length == 0 ? instructorSlot.InstructorInitials : cellData + $"<BR>{instructorSlot.InstructorInitials}";
                    }
                }

                foreach(var cell in slotRowData)
                {
                    htmlPage.Append("<td>");
                    htmlPage.Append(cell);
                    htmlPage.Append("</td>");
                }

                htmlPage.Append("</tr>");
            }

            htmlPage.AppendLine("</tbody>");
            htmlPage.AppendLine("</table>");
            htmlPage.AppendLine("<br>");
            htmlPage.AppendLine($"<div><span class='bold'>Total free slots: </span><span>{calendarDays.CalculateTotalSlotCount()}</span></div>");
            htmlPage.AppendLine("</body>");
            htmlPage.AppendLine("</html>");

            await _storageHelper.SaveFileAsync(ContainerNames.Web, "instructors-and-slots.html", htmlPage.ToString(), cancellationToken);
        }
    }
}
