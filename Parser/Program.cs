﻿using CalendarBackendLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        private static readonly DirectoryInfo BASE_DIRECTORY = new DirectoryInfo(Environment.CurrentDirectory);
        private static readonly DirectoryInfo CALENDAR_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("calendars");
        private static readonly DirectoryInfo EVENT_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("eventjsons");
        private static readonly DirectoryInfo USERCONFIG_DIRECTORY = BASE_DIRECTORY.CreateSubdirectory("userconfig");

        static void Main(string[] args)
        {
            Log("Start FileSystemWatcher...");
            var eventFileWatcher = new FileSystemWatcher(EVENT_DIRECTORY.FullName, "*.json");
            eventFileWatcher.Changed += EventFileChanged;
            eventFileWatcher.Created += EventFileChanged;
            eventFileWatcher.EnableRaisingEvents = true;

            var userconfigFileWatcher = new FileSystemWatcher(USERCONFIG_DIRECTORY.FullName, "*.json");
            userconfigFileWatcher.Changed += UserconfigFileChanged;
            userconfigFileWatcher.Created += UserconfigFileChanged;
            userconfigFileWatcher.EnableRaisingEvents = true;
            Log("FileSystemWatcher started.");

            Log("Generate all Userconfigs...");
            var generateAllTask = GenerateAllUserconfigs();
            Log("All generated...");

            while (true)
            {
                var deleteTask = DeleteNotAnymoreNeededCalendars();
                Log("Now wait for changes...");
                System.Threading.Thread.Sleep(1000 * 60 * 60); // 1 hour
            }
        }

        private static void Log(string text)
        {
            Console.WriteLine(text);
        }

        private static async void EventFileChanged(object sender, FileSystemEventArgs e)
        {
            Log("EventFile " + e.Name + " change detected.");

            try
            {
                var fileinfo = new FileInfo(e.FullPath);
                var eventname = fileinfo.Name.Replace(".json", "");

                var userconfigs = await GetAllUserconfigs();
                var relevantUserconfigs = userconfigs
                    .Where(o => o.config.events
                        .Select(EventEntry.GetFilename)
                        .Contains(e.Name)
                    )
                    .ToArray();

                await GenerateSetOfUserconfigs(relevantUserconfigs);
            }
            catch (Exception ex)
            {
                Log("Could not generate based on EventFile " + e.Name + ": " + ex.Message);
            }
        }

        private static async void UserconfigFileChanged(object sender, FileSystemEventArgs e)
        {
            Log("Userconfig " + e.Name + " change detected.");

            try
            {
                var userconfigJson = await File.ReadAllTextAsync(e.FullPath);
                var userconfig = JsonHelper.ConvertFromJson<Userconfig>(userconfigJson);

                var result = await GenerateCalendar(userconfig);
                Log("Generated " + e.Name + " new: " + result.ChangeState);
            }
            catch (Exception ex)
            {
                Log("Could not generate Userconfig " + e.Name + ": " + ex.Message);
            }
        }

        private static async Task GenerateAllUserconfigs()
        {
            var userconfigs = await GetAllUserconfigs();
            await GenerateSetOfUserconfigs(userconfigs);
        }

        private static async Task DeleteNotAnymoreNeededCalendars()
        {
            Log("find unneeded calendars...");

            var userconfigs = await GetAllUserconfigs();
            var allIds = userconfigs
                .Select(o => o.chat.id.ToString())
                .ToArray();

            var test = CALENDAR_DIRECTORY.EnumerateFiles("*.ics")
                .Where(o => !allIds.Contains(o.Name.Replace(".ics", "")))
                .ToArray();

            if (test.Length == 0)
            {
                Log("nothing to delete.");
                return;
            }

            Log(test.ToArrayString("delete"));
            foreach (var item in test)
            {
                item.Delete();
            }
            Log("Delete finished.");
        }

        private static async Task GenerateSetOfUserconfigs(IEnumerable<Userconfig> userconfigs)
        {
            Log(userconfigs.Select(o => o.chat.first_name).ToArrayString("Generate new"));

            var changeStates = await userconfigs
                .Select(GenerateCalendar)
                .WhenAll();

            if (changeStates.CountCreated() > 0)
                Log(changeStates.OnlyCreated().ToArrayString("Created"));
            if (changeStates.CountChanged() > 0)
                Log(changeStates.OnlyChanged().ToArrayString("Changed"));
            if (changeStates.CountUnchanged() > 0)
                Log(changeStates.OnlyUnchanged().ToArrayString("Unchanged"));
        }

        private static async Task<Userconfig[]> GetAllUserconfigs()
        {
            var userconfigJsons = await USERCONFIG_DIRECTORY.EnumerateFiles("*.json")
                .Select(o => File.ReadAllTextAsync(o.FullName))
                .WhenAll();

            var userconfigs = userconfigJsons
                .Select(o => JsonHelper.ConvertFromJson<Userconfig>(o))
                .ToArray();

            return userconfigs;
        }

        private static async Task<ChangedObject> GenerateCalendar(Userconfig config)
        {
            return await GenerateCalendar(config.chat.first_name, config.chat.id.ToString(), config.config.events);
        }

        private static async Task<ChangedObject> GenerateCalendar(string name, string filename, params string[] eventnames)
        {
            var events = await LoadEventsByName(eventnames);

            var icsContent = IcsGenerator.GenerateIcsContent(name, events);

            var file = new FileInfo(CALENDAR_DIRECTORY.FullName + Path.DirectorySeparatorChar + filename + ".ics");

            if (file.Exists)
            {
                var currentFileContent = await File.ReadAllTextAsync(file.FullName);
                if (currentFileContent == icsContent)
                    return new ChangedObject(name, ChangeState.Unchanged);
            }

            var changeState = file.Exists ? ChangeState.Changed : ChangeState.Created;
            await File.WriteAllTextAsync(file.FullName, icsContent);
            return new ChangedObject(name, changeState);
        }

        private static async Task<EventEntry[]> LoadEventsByName(params string[] eventnames)
        {
            var filenamesOfEvents = eventnames.Select(EventEntry.GetFilename).ToArray();

            var eventJsons = await EVENT_DIRECTORY.EnumerateFiles("*.json")
                .Where(o => filenamesOfEvents.Contains(o.Name))
                .Select(o => File.ReadAllTextAsync(o.FullName))
                .WhenAll();

            var events = eventJsons
                .SelectMany(o => JsonHelper.ConvertFromJson<EventEntry[]>(o))
                .ToArray();

            return events;
        }

    }
}