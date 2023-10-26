﻿using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaccination;

// Samuel Lööf & Simon Sörqvist, uppgift 4

namespace Schedule
{
    public class Info
    {
        //private DateTime _StartDate { get; set; }
        public DateTime StartDate { get; set; }
        /*
        {
            get { return _StartDate; }

            set
            {
                // updates startdate hours/mins/seconds when the value is changed 
                _StartDate = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0);
                _StartDate.Add(StartTime);
            }
        } */
        //private TimeSpan _StartTime { get; set; }
        public TimeSpan StartTime { get; set; }
        /*
        {
            get { return _StartTime; }

            set
            {
                _StartTime = value;

                // update startdate with new hours/mins/seconds when starttime is changed 
                _StartDate = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 0, 0, 0);
                _StartDate.Add(value);
            }
        } */
        public TimeSpan EndTime { get; set; }
        public TimeSpan VaccinationTime { get; set; }
        public int ConcurrentVaccinations { get; set; }
        public string FilePathICS { get; set; }

        public Info()
        {
            StartDate = DateTime.Today.AddDays(7);
            StartTime = new TimeSpan(8, 0, 0);
            EndTime = new TimeSpan(20, 0, 0);
            ConcurrentVaccinations = 2;
            VaccinationTime = new TimeSpan(0, 5, 0);
            FilePathICS = "C:\\Windows\\Temp\\Schedule.ics";
        }
    }

    public class SubMenu
    {
        // method for scheduling vaccinations, main menu points here and treats this as a sub-menu 
        public static Info ScheduleMenu(Info schedule)
        {
           
             //The schedule should be saved in a .Ics file.
             

            var newSchedule = schedule;

            while (true)
            {
                Console.WriteLine("Schemalägg vaccinationer");
                Console.WriteLine("--------------------");
                Console.WriteLine("Mata in blankrad för att välja standardvärde.");

                int scheduleMenu = Vaccination.Program.ShowMenu("", new[]
                {
                    $"Startdatum: {newSchedule.StartDate.ToString("yyyy-MM-dd")}",
                    $"Starttid: {newSchedule.StartTime.ToString("hh\\:mm")}",
                    $"Sluttid: {newSchedule.EndTime.ToString("hh\\:mm")}",
                    $"Antal samtidiga vaccinationer: {newSchedule.ConcurrentVaccinations}",
                    $"Minuter per vaccination: {newSchedule.VaccinationTime.TotalMinutes}",
                    $"Kalenderfil: {newSchedule.FilePathICS}",
                    "Gå tillbaka till huvudmeny"
                });

                Console.Clear();

                if (scheduleMenu == 0) //Change the start date for vaccinations
                {
                    newSchedule.StartDate = VaccinationStartDate();
                }
                else if (scheduleMenu == 1) //Change the start time for vaccinations
                {
                    newSchedule.StartTime = VaccinationStartTime();
                }

                else if (scheduleMenu == 2) //Change the the end time for vacciantions
                {
                    newSchedule.EndTime = VaccinationEndTime();
                }
                else if (scheduleMenu == 3) //Change the number of people that's allowed to get vaccinated at the same time
                {
                    newSchedule.ConcurrentVaccinations = ConcurrentVaccinations();
                }
                else if (scheduleMenu == 4) //Change how many minutes each vaccination should take.
                {
                    newSchedule.VaccinationTime = VaccinatonDuration();
                }
                else if (scheduleMenu == 5) //Choose where to save the calendar .ics file.
                {
                    Console.WriteLine("Var vill du att .ics filen ska sparas?");

                    newSchedule.FilePathICS = ChangeFilePathICS();
                }
                else { return newSchedule; } // exits this sub-menu and goes back to main-menu (main-loop) 
            }
        }
        
        public static string ChangeFilePathICS()
        {
            while (true)
            {
                Console.WriteLine("(Ex.: C:\\Windows\\Temp\\exempel.ics)");
                Console.WriteLine("---------------");
                Console.Write("Ny filsökväg: ");
                string newPath = Console.ReadLine().Trim();

                Console.Clear();

                if (Path.IsPathFullyQualified(newPath))
                {
                    // get file-extension if there is one
                    string fileName = Path.GetFileName(newPath);
                    string fileExtension = fileName.Substring(fileName.LastIndexOf('.') + 1);

                    string tempPath = newPath.Substring(0, newPath.LastIndexOf("\\"));
                    if (Directory.Exists(tempPath))
                    {
                        if (fileExtension == "ics" || fileExtension == "ICS")
                        {
                            return newPath;
                        }
                    }
                }

                // tell user to try again
                Console.WriteLine("Sökvägen du angett är ogiltig, ange en giltig filsökväg.");
                Console.WriteLine("Tänk på att välja rätt fil-ändelse (.ics/.ICS)");
                Console.WriteLine();
            }
        }

        // takes vaccination priority order as input (string[]) and returns the lines for the ics file
        public static string[] PriorityOrderToICS(string[] priorityOrder, Info scheduleInfo)
        {
            var outputICS = new List<string>(); // output list

            /*
             * ics file output raw-text should look like this (contains 2 separate events):
             * 
                BEGIN:VCALENDAR
                VERSION:2.0
                PRODID:-//hacksw/handcal//NONSGML v1.0//EN

                BEGIN:VEVENT
                UID:20231101T080000Z@example.com
                DTSTAMP:20231101T080000Z
                DTSTART:20231101T080000Z
                DTEND:20231101T080500Z
                SUMMARY:Namn,Namnsson,19950202-2244,Doser: 1
                END:VEVENT

                BEGIN:VEVENT
                UID:20231101T080500Z@example.com
                DTSTAMP:20231101T080500Z
                DTSTART:20231101T080500Z
                DTEND:20231101T081000Z
                SUMMARY:Namn,Namnsson,19900101-1122,Doser: 2
                END:VEVENT

                END:VCALENDAR
             */

            // add "start-template" values to outputICS 
            outputICS.Add("BEGIN:VCALENDAR");
            outputICS.Add("VERSION:2.0");
            outputICS.Add("PRODID:-//hacksw/handcal//NONSGML v1.0//EN");

            DateTime currentDate = scheduleInfo.StartDate.Add(scheduleInfo.StartTime);
            DateTime timeLimit = scheduleInfo.StartDate.Add(scheduleInfo.EndTime);

            foreach (string vaccination in priorityOrder)
            {
                DateTime tempDate = currentDate.Add(scheduleInfo.VaccinationTime);
                if (tempDate < timeLimit)
                {
                    // [DO THE VACCINATION HERE]

                    // add time so the next vaccination is scheduled correctly 
                    currentDate.Add(scheduleInfo.VaccinationTime); 
                }
                else
                {
                    // dont do the vaccination 

                    currentDate.AddDays(1);
                    timeLimit.AddDays(1);
                }

                outputICS.Add("BEGIN:VEVENT");

                

                outputICS.Add("END:VEVENT");
            }

            outputICS.Add("END:VCALENDAR"); // ends ics file-template 

            return outputICS.ToArray();
        }

        public static DateTime VaccinationStartDate()
        {
            while (true)
            {
                Console.WriteLine("Ange nytt startdatum (YYYY-MM-DD): ");
                string input = Console.ReadLine();
                Console.Clear();

                if (string.IsNullOrEmpty(input))
                {
                    return DateTime.Today.AddDays(7); // Set it to default value
                }

                try
                {
                    var startDate = DateTime.ParseExact(input, "yyyy-MM-dd", null);
                    return startDate;
                }
                catch
                {
                    Console.WriteLine("Felaktigt datumformat. Använd formatet: YYYY-MM-DD (år-månad-dag).");
                }
            }
        }

        public static TimeSpan VaccinationStartTime()
        {
            while (true)
            {
                Console.WriteLine("Ange ny starttid. t.ex.: 12:00");
                string input = Console.ReadLine();
                Console.Clear();

                if (string.IsNullOrEmpty(input))
                {
                    return new TimeSpan(8, 0, 0); // Set it to default value
                }

                try
                {
                    DateTime time = DateTime.ParseExact(input, "HH:mm", null);
                    return time.TimeOfDay;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Felaktigt tidsformat. Använd formatet: HH:mm (timmar:minuter).");
                }
            }
        }

        public static TimeSpan VaccinationEndTime()
        {
            while (true)
            {
                Console.WriteLine("Ange ny sluttid. t.ex.: 20:00");
                string input = Console.ReadLine();
                Console.Clear();

                if (string.IsNullOrEmpty(input))
                {
                    return new TimeSpan(20, 0, 0); // Set it to default value
                }

                try
                {
                    DateTime time = DateTime.ParseExact(input, "HH:mm", null);
                    return time.TimeOfDay;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Felaktigt tidsformat. Använd formatet: HH:mm (timmar:minuter).");
                }
            }
        }

        public static int ConcurrentVaccinations()
        {
            while (true)
            {
                Console.WriteLine("Hur många personer ska kunna vaccineras samtidigt?");
                string input = Console.ReadLine();
                Console.Clear();

                if (string.IsNullOrEmpty(input))
                {
                    return 2; // Set it to default value
                }

                int inputAsNr = 0;

                try
                {
                    inputAsNr = int.Parse(input);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Felaktigt format. Vänligen ange ett heltal.");
                    Console.WriteLine();
                    continue;
                }

                if (inputAsNr > 0)
                {
                    return inputAsNr;
                }
                else
                {
                    Console.WriteLine("Felaktigt format. Vänligen ange ett positivt heltal.");
                    Console.WriteLine();
                }
            }
        }

        public static TimeSpan VaccinatonDuration()
        {
            
            while (true)
            {
                Console.WriteLine("Hur länge ska varje vaccination vara (i minuter)?");
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.Clear();
                    return new TimeSpan(0, 5, 0); // Set it to default value
                }
                try
                {
                    int minutes = int.Parse(input);
                    if (minutes > 0)
                    {
                        Console.Clear();
                        TimeSpan vaccinationTime = new TimeSpan(0, minutes, 0);
                        return vaccinationTime;                 
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Felaktigt tidsformat. Ange ett positivt heltal.");
                        Console.WriteLine();
                    }
                }
                catch (FormatException)
                {
                    Console.Clear();
                    Console.WriteLine("Felaktigt tidsformat. Ange vaccinationtiden i minuter.");
                    Console.WriteLine();
                }
            }
        }
    }
}
