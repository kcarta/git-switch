using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace GitSwitch
{
    class Program
    {
        private const string GitExeName = "git.exe";
        private const string GitNewNameCommand = "config --global user.name ";
        private const string GitNewEmailCommand = "config --global user.email ";
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(List<Person>));

        private const string FileName = "gitusers.xml";
        private static readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FileName);

        static void Main(string[] args)
        {
            // Example: $ gitswitch -n jqp "John Q. Person" jqp@example.com
            if (args.Length == 4 && args[0] == "-n" && args[1].Length == 3)
            {
                RegisterNew(args[1].ToLower(), args[2], args[3]);
            }
            // Example: $ gitswitch jqp
            else if (args.Length == 1 && args[0].Length == 3)
            {
                SwitchUser(args[0].ToLower());
            }
            else
            {
                PrintUsage();
            }
        }

        private static void RegisterNew(string initials, string name, string email)
        {
            // If we got back a null result, just use an empty list and save that with the new info
            var people = LoadListFromFile() ?? new List<Person>();
            if (people.Exists(person => person.Initials == initials))
            {
                Console.WriteLine($"{initials} already registered");
                PrintUsage();
            }
            else
            {
                people.Add(new Person { Initials = initials, Name = name, Email = email });
                WriteListToFile(people);
            }
        }

        private static List<Person> LoadListFromFile()
        {
            try
            {
                using (var fileStream = File.Open(_filePath, FileMode.OpenOrCreate))
                {
                    return _serializer.Deserialize(fileStream) as List<Person>;
                }
            }
            // A failure here means that deserialization failed, so we'll just return a null
            catch (Exception)
            {
                return null;
            }
        }

        private static void WriteListToFile(List<Person> people)
        {
            using (var streamWriter = new StreamWriter(_filePath, false))
            {
                _serializer.Serialize(streamWriter, people);
            }
        }

        private static void SwitchUser(string initials)
        {
            var person = FindPerson(initials);
            if (person == null)
            {
                Console.WriteLine($"{initials} not registered");
                PrintUsage();
            }
            else
            {
                ExecuteGitSwitchCommand(person);
            }
        }

        private static Person FindPerson(string initials)
        {
            var people = LoadListFromFile();
            if (people == null)
            {
                Console.WriteLine($"Error reading data file: {_filePath}");
                return null;
            }
            return people.Find(p => p.Initials == initials);
        }

        private static void ExecuteGitSwitchCommand(Person person)
        {
            try
            {
                using (Process gitProcess = new Process())
                {
                    gitProcess.StartInfo = new ProcessStartInfo() { FileName = GitExeName };
                    gitProcess.StartInfo.Arguments = GitNewNameCommand + $"\"{person.Name}\""; 
                    gitProcess.Start();
                    gitProcess.WaitForExit();

                    gitProcess.StartInfo.Arguments = GitNewEmailCommand + person.Email;
                    gitProcess.Start();
                    gitProcess.WaitForExit();
                }
            }
            // If we fail here, it is because Git was not found.
            catch (Exception)
            {
                Console.WriteLine("Git executable not found. Make sure Git is installed and in the path variable");
            }
        }

        private static void PrintUsage()
        {
            string usageText = "\tusage: gitswitch initials\n" +
                "\t\tSwitches the global git configuration to a registered user with matching initials.\n" +
                "\t\tInitials must be exactly three letters.\n" +
                "\toptions:\n" +
                "\t\t-n initials \"first-name last-name\" email\n" +
                "\t\t\tRegisters a new user with the provided initials\n" +
                "\t\t\tInitials must be exactly three letters.\n";
            Console.WriteLine(usageText);
        }
    }
    public class Person
    {
        public string Initials { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
