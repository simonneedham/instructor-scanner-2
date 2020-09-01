using System;
using System.Collections.Generic;

namespace InstructorScanner2.FunctionApp
{
    public class CalendarDay
    {
        public string Id { get; set; }

        public DateTimeOffset Date { get; set; }

        public List<InstructorSlots> InstructorSlots { get; set; }

        public string FlyingClub { get; set; } = "FlyingClub";

        public int Ttl { get; set; }

        public DateTimeOffset Created { get; set; }

        public void SetDate(DateTime date)
        {
            Created = DateTimeOffset.Now;
            Date = date;
            Id = date.ToString("yyyyMMdd");
            Ttl= (date - DateTime.Today).Days * 24 * 60 * 60;
        }
    }
}
