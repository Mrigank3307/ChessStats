﻿using ChessStats.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessStats
{
    class Program
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        static async Task Main(string[] args)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string chessdotcomUsername = args[0];

            Helpers.DisplayLogo();
            Helpers.displaySection($"Fetching Games for {chessdotcomUsername}", true);

            System.Console.WriteLine($">>Starting ChessDotCom Fetch");

            var gameList = PgnFromChessDotCom.FetchGameRecordsForUser(chessdotcomUsername);

            System.Console.WriteLine();
            System.Console.WriteLine($">>Finished ChessDotCom Fetch");
            System.Console.WriteLine($">>Processing Games");

            SortedList<string, int> secondsPlayedRollup = new SortedList<string, int>();
            SortedList<string, int> ecoPlayedRollupWhite = new SortedList<string, int>();
            SortedList<string, int> ecoPlayedRollupBlack = new SortedList<string, int>();
            double totalSecondsPlayed = 0;

            foreach (var game in gameList)
            {
                // Don't include daily games
                if (game.GameAttributes.Attributes["Event"] != "Live Chess") continue;

                var side = game.GameAttributes.Attributes["White"].ToUpperInvariant() == chessdotcomUsername.ToUpperInvariant() ? "White" : "Black";

                try
                {
                    var ecoName = game.GameAttributes.Attributes["ECOUrl"].Replace(@"https://www.chess.com/openings/", "").Replace("-", " ");
                    var ecoShortened = new Regex(@"^.*?(?=[0-9])").Match(ecoName).Value.Trim();
                    var ecoKey = $"{game.GameAttributes.Attributes["ECO"]}-{((string.IsNullOrEmpty(ecoShortened)) ? ecoName : ecoShortened)}";
                    var ecoPlayedRollup = (side == "White") ? ecoPlayedRollupWhite : ecoPlayedRollupBlack;

                    if (ecoPlayedRollup.ContainsKey(ecoKey))
                    {
                        ecoPlayedRollup[ecoKey]++;
                    }
                    else
                    {
                        ecoPlayedRollup.Add(ecoKey, 1);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    //ECO missing from Pgn so just ignore
                }

                var gameStartDate = game.GameAttributes.Attributes["Date"];
                var gameStartTime = game.GameAttributes.Attributes["StartTime"];
                var gameEndDate = game.GameAttributes.Attributes["EndDate"];
                var gameEndTime = game.GameAttributes.Attributes["EndTime"];

                DateTime parsedStartDate;
                DateTime parsedEndDate;

                var startDateParsed = DateTime.TryParseExact($"{gameStartDate} {gameStartTime}", "yyyy.MM.dd HH:mm:ss", null, DateTimeStyles.AssumeUniversal, out parsedStartDate);
                var endDateParsed = DateTime.TryParseExact($"{gameEndDate} {gameEndTime}", "yyyy.MM.dd HH:mm:ss", null, DateTimeStyles.AssumeUniversal, out parsedEndDate);
                var seconds = System.Math.Abs((parsedEndDate - parsedStartDate).TotalSeconds);
                var gameTime = $"{game.TimeClass}{((game.IsRatedGame) ? "" : " Unrated")}";


                string key = $"{parsedStartDate.Year}-{((parsedStartDate.Month < 10) ? "0" : "")}{parsedStartDate.Month} {gameTime}";

                totalSecondsPlayed += seconds;

                if (secondsPlayedRollup.ContainsKey(key))
                {
                    secondsPlayedRollup[key] += (int)seconds;
                }
                else
                {
                    secondsPlayedRollup.Add(key, (int)seconds);
                }
            }

            System.Console.WriteLine($">>Finished Processing Games");
            System.Console.WriteLine("");
            Helpers.displaySection($"Live Chess Report for {chessdotcomUsername} - {DateTime.Now.ToShortDateString()}", true);
            Console.WriteLine("");
            Helpers.displaySection("Openings Playing As White >1 (Max 15)", false);
            foreach (var ecoCount in ecoPlayedRollupWhite.OrderByDescending(uses => uses.Value).Take(15))
            {
                if (ecoCount.Value < 2) { break; }
                Console.WriteLine($"{ecoCount.Key.PadRight(75, ' ')} | {ecoCount.Value.ToString().PadLeft(4)}");
            }

            Console.WriteLine("");
            Helpers.displaySection("Openings Playing As Black >1 (Max 15)", false);
            foreach (var ecoCount in ecoPlayedRollupBlack.OrderByDescending(uses => uses.Value).Take(15))
            {
                if (ecoCount.Value < 2) { break; }
                Console.WriteLine($"{ecoCount.Key.PadRight(75, ' ')} | {ecoCount.Value.ToString().PadLeft(4)}");
            }

            Console.WriteLine("");
            Helpers.displaySection("Time Played by Month/Time Control", false);
            Console.WriteLine("Month/TimeClass        | Play Time |            ");
            Console.WriteLine("-----------------------+-----------+------------");
            foreach (var rolledUp in secondsPlayedRollup)
            {
                TimeSpan timeMonth = TimeSpan.FromSeconds(rolledUp.Value);
                System.Console.WriteLine($"{rolledUp.Key.PadRight(22, ' ')} | {((int)timeMonth.TotalHours).ToString().PadLeft(3, ' ')}:{ timeMonth.Minutes.ToString().PadLeft(2, '0')}:{ timeMonth.Seconds.ToString().PadLeft(2, '0')} | {rolledUp.Value} seconds");
            }

            Console.WriteLine("");
            Helpers.displaySection("Total Play Time (Live Chess)", false);
            TimeSpan time = TimeSpan.FromSeconds(totalSecondsPlayed);
            Console.WriteLine($"Time Played (hh:mm:ss): {((int)time.TotalHours).ToString().PadLeft(3, ' ')}:{ time.Minutes.ToString().PadLeft(2, '0')}:{ time.Seconds.ToString().PadLeft(2, '0')}");
            Console.WriteLine("");
            Helpers.PressToContinue();
        }
    }
}
