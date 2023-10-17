﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

// Samuel Lööf & Simon Sörqvist, uppgift 4

/* 
 * hej :)
 */

namespace Vaccination
{
    public class Person
    {
        public string IdentificationNumber = "20200101-1111";
        public string FirstName = "Brad";
        public string LastName = "Pitt";
        public bool WorksInHealthcare = false;
        public bool IsInRiskGroup = false;
        public bool HasHadInfection = false;
        //public bool HasHadFirstDose = false;
    }
    public class Program
    {
        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int doses = 0;
            bool vaccinateChildren = false;

            string inputCSVFilepath = string.Empty;
            string outputCSVFilepath = string.Empty;

            while (true)
            {
                Console.Clear();

                Console.WriteLine("Huvudmeny");
                Console.WriteLine("----------");
                Console.WriteLine($"Antal tillängliga vaccindoser {doses}");

                string ageRestriction = vaccinateChildren ? "ja" : "nej";
                Console.WriteLine($"Vaccinering under 18 år: {ageRestriction}");
                Console.WriteLine($"Indatafil: {inputCSVFilepath}");
                Console.WriteLine($"Utdatafil: {outputCSVFilepath}");
                Console.WriteLine();

                int mainMenu = ShowMenu("Vad vill du göra?", new[]
                {
                    "Skapa prioritetsordning ",
                    "Schemalägg vaccinationer", // <-- fr. VG-delen 
                    "Ändra antal vaccindoser",
                    "Ändra åldersgräns",
                    "Ändra indatafil",
                    "Ändra utdatafil",
                    "Avsluta"
                });
                Console.Clear();

                if (mainMenu == 0)
                {
                    // Prioritesordning
                }
                else if (mainMenu == 1)
                {
                    // Schemalägg vaccinationer
                    // schemalägg är fr. VG-delen 
                }

                else if (mainMenu == 2)
                {
                    doses = ChangeVaccineDosages();
                }
                else if (mainMenu == 3)
                {
                    vaccinateChildren = ChangeAgeRequirement();
                }
                else if (mainMenu == 4)
                {
                    inputCSVFilepath = ChangeFilePath(isOutputFilePath: false);
                }
                else if (mainMenu == 5)
                {
                    outputCSVFilepath = ChangeFilePath(isOutputFilePath: true);
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Exiting program. Goodbye!");
                    Console.WriteLine();
                    break; // breaks main-loop 
                }
            } // <-- end of Main-loop 
        } // <-- end of Main() 

        public static int ChangeVaccineDosages()
        {
            while (true)
            {
                Console.WriteLine("Ändra antal vaccindoser");
                Console.WriteLine("-----------------");
                Console.Write("Ange nytt antal doser: ");

                try
                {
                    int newVaccineDosages = int.Parse(Console.ReadLine());
                    Console.WriteLine($"Nytt antal vaccindoser: {newVaccineDosages}");
                    return newVaccineDosages; // Return the new value of vaccine dosages, changed by the user.
                }
                catch (FormatException)
                {
                    Console.Clear();
                    Console.WriteLine("Vänligen ange vaccindoseringarna i heltal.");
                    Console.WriteLine();
                }
            }
        }

        public static bool ChangeAgeRequirement()
        {
            int ageMenu = ShowMenu("Ska personer under 18 vaccineras?", new[]
            {
                "Ja",
                "Nej"
            });

            //Returns the new updated vaccination age. 
            if (ageMenu == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // ChangeFilePath lets the user enter a filepath and makes sure it is valid
        public static string ChangeFilePath(bool isOutputFilePath)
        {
            while (true)
            {
                if (isOutputFilePath)
                {
                    Console.WriteLine("Ändra utdatafil.");
                }
                else
                {
                    Console.WriteLine("Ändra indatafil.");
                }

                Console.WriteLine("---------------");
                Console.Write("Ny filsökväg: ");
                string newPath = Console.ReadLine().Trim();

                if (Path.IsPathFullyQualified(newPath))
                {
                    // output does not work haHAA
                    if (isOutputFilePath) 
                    {
                        string tempPath = newPath.Replace(Path.GetFileName(newPath), "");
                        if (Directory.Exists(tempPath)) { return newPath; }
                    }
                    else
                    {
                        if (File.Exists(newPath)) { return newPath; }
                    }
                }
                
                Console.Clear();
                Console.WriteLine("Mappen eller filen finns inte, ange en giltig filsökväg.");
                Console.WriteLine();
            }
        }
    
    

    // Create the lines that should be saved to a CSV file after creating the vaccination order.
    //
    // Parameters:
    //
    // input: the lines from a CSV file containing population information
    // doses: the number of vaccine doses available
    // vaccinateChildren: whether to vaccinate people younger than 18
    public static string[] CreateVaccinationOrder(string[] input, int doses, bool vaccinateChildren)
        {

            //Read the people info from the CSV file. Example of the formats from the CSV file should look like this :19720906-1111,Elba,Idris,0,0,1 (and) 8102032222,Efternamnsson,Eva,1,1,0
            //Maybe split boh commas and dashes?

            /* 1. Works in health care
             * 2. age 65+ (yymmdd)
             * 3. Risk group.
             * 4. rest of population
             * 5. If the user wishes to vaccinate children (under age of 18) then treat them as an adult in the priority list.
             * 
             * People who have been infected already should be vaccinated with only one dose. Rest with two.
             * If there is only one dose left and the next person in the order requires two dosages then this person shouldn't get any dossages at all.
             * And same goes for the rest of the people after this person even if they only require one dosage.
             *if theres one dose left and the next person only requires one dose they should still be vaccinated.
             * The supply of vaccine dosages should not be allowed to get changed after a priority order have been made,
             * unless the user changes the available dosages them self from the menu.
             * 
             
             * 
             */

            // Read and parse the CSV data from the input array
            List<Person> people = new List<Person>();

            foreach (string line in input)
            {
                string[] values = line.Split(new[] {','});

                if (values.Length >= 6) // Make sure there are at least 6 values in the array.
                {
                    string identificationNumber = values[0];
                    string firstName = values[1];
                    string lastName = values[2];
                    bool worksInHealthcare = values[3] == "1";
                    bool isInRiskGroup = values[4] == "1";
                    bool hasHadInfection = values[5] == "1";

                    // If the identification number contains dashes, remove them.
                    identificationNumber = identificationNumber.Replace("-", "");

                    // Create a Person object
                    Person person = new Person
                    {
                        IdentificationNumber = identificationNumber,
                        FirstName = firstName,
                        LastName = lastName,
                        WorksInHealthcare = worksInHealthcare,
                        IsInRiskGroup = isInRiskGroup,
                        HasHadInfection = hasHadInfection
                    };

                    // Store the person in the list
                    people.Add(person);

                }     
            }
            // Sort the people based on the vaccination priority criteria
            List<Person> vaccinationOrder = SortVaccinationOrder(people, vaccinateChildren);


            return new string[0];
        }

        public static List<Person> SortVaccinationOrder(List<Person> people, bool vaccinateChildren)
        {

            return people

            //Priority order for vaccination:
           .OrderByDescending(p => p.WorksInHealthcare) //1. If the person works in healthcare
           .ThenBy(p => p.age >= 65) // 2.people aged 65 and older
           .ThenByDescending(p => p.IsInRiskGroup) //3. If the person is in a risk group.
           .ThenByDescending(p => p.Aae) //4. Then by age in order.
                .ToList();


        }

        public static string IDNumberToAge(string identificationnumber)
        {
            // Remove any dashes or other non-digit characters
            identificationnumber = new string(identificationnumber.Where(char.IsDigit).ToArray());

            string yearPart, monthPart, dayPart;


            if (identificationnumber.Length == 10)
            {
                yearPart = identificationnumber.Substring(0, 2);
                monthPart = identificationnumber.Substring(2, 2);
                dayPart = identificationnumber.Substring(4, 2);
            }
            else if (identificationnumber.Length == 12)
            {
                yearPart = identificationnumber.Substring(0, 4);
                monthPart = identificationnumber.Substring(4, 2);
                dayPart = identificationnumber.Substring(6, 2);
            }
            else
            {
                Console.WriteLine("Invalid identification number length");
            }
            //Get the current date
            DateTime currentdate = DateTime.Now;

            int year = int.Parse(yearPart);
            int month = int.Parse(monthPart);
            int day = int.Parse(dayPart);



        }
    

        public static int ShowMenu(string prompt, IEnumerable<string> options)
        {
            if (options == null || options.Count() == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty list of options.");
            }

            Console.WriteLine(prompt);

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            // Calculate the width of the widest option so we can make them all the same width later.
            int width = options.Max(option => option.Length);

            int selected = 0;
            int top = Console.CursorTop;
            for (int i = 0; i < options.Count(); i++)
            {
                // Start by highlighting the first option.
                if (i == 0)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                var option = options.ElementAt(i);
                // Pad every option to make them the same width, so the highlight is equally wide everywhere.
                Console.WriteLine("- " + option.PadRight(width));

                Console.ResetColor();
            }
            Console.CursorLeft = 0;
            Console.CursorTop = top - 1;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                key = Console.ReadKey(intercept: true).Key;

                // First restore the previously selected option so it's not highlighted anymore.
                Console.CursorTop = top + selected;
                string oldOption = options.ElementAt(selected);
                Console.Write("- " + oldOption.PadRight(width));
                Console.CursorLeft = 0;
                Console.ResetColor();

                // Then find the new selected option.
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Count() - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }

                // Finally highlight the new selected option.
                Console.CursorTop = top + selected;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                string newOption = options.ElementAt(selected);
                Console.Write("- " + newOption.PadRight(width));
                Console.CursorLeft = 0;
                // Place the cursor one step above the new selected option so that we can scroll and also see the option above.
                Console.CursorTop = top + selected - 1;
                Console.ResetColor();
            }

            // Afterwards, place the cursor below the menu so we can see whatever comes next.
            Console.CursorTop = top + options.Count();

            // Show the cursor again and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
    }

    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void exTest()
        {

        }
    }

    // Jakobs tests vv
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void BaseFunctionalityTest()
        {
            // Arrange
            string[] input =
            {
                "19720906-1111,Elba,Idris,0,0,1",
                "8102032222,Efternamnsson,Eva,1,1,0"
            };
            int doses = 10;
            bool vaccinateChildren = false;

            // Act
            string[] output = Program.CreateVaccinationOrder(input, doses, vaccinateChildren);

            // Assert
            Assert.AreEqual(output.Length, 2);
            Assert.AreEqual("19810203-2222,Efternamnsson,Eva,2", output[0]);
            Assert.AreEqual("19720906-1111,Elba,Idris,1", output[1]);
        }
    }
    // Jakobs tests ^^^
}
