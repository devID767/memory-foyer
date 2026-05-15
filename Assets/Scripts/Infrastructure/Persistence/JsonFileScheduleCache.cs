using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Time;
using MemoryFoyer.Infrastructure.Dtos;
using UnityEngine;

namespace MemoryFoyer.Infrastructure.Persistence
{
    public sealed class JsonFileScheduleCache : IScheduleCache
    {
        private readonly string _schedulesDir;
        private readonly string _pendingDir;
        private readonly string _summariesPath;
        private readonly IClock _clock;

        public JsonFileScheduleCache(string rootPath, IClock clock)
        {
            _schedulesDir = Path.Combine(rootPath, "schedules");
            _pendingDir = Path.Combine(rootPath, "pending");
            _summariesPath = Path.Combine(rootPath, "deck-summaries.json");
            _clock = clock;
        }

        public void Save(DeckSchedule schedule)
        {
            CardSchedule[] cards = schedule.Cards is CardSchedule[] arr
                ? arr
                : schedule.Cards.ToArray();

            CardScheduleDto[] cardDtos = new CardScheduleDto[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                cardDtos[i] = ScheduleMappers.ToDto(cards[i]);
            }

            DeckScheduleDto dto = new DeckScheduleDto
            {
                deckId = schedule.DeckId.Value,
                cards = cardDtos,
            };

            string json = JsonUtility.ToJson(dto);
            WriteAtomic(SchedulePath(schedule.DeckId), json);
        }

        public DeckSchedule? Load(DeckId deckId)
        {
            string path = SchedulePath(deckId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                DeckScheduleDto dto = JsonUtility.FromJson<DeckScheduleDto>(json);
                return ScheduleMappers.FromDto(dto, _clock, ScheduleSource.Cache);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cache] Corrupt schedule for {deckId}: {ex.Message}");
                return null;
            }
        }

        public bool Has(DeckId deckId)
        {
            return File.Exists(SchedulePath(deckId));
        }

        public void SaveDeckSummaries(IReadOnlyList<DeckSummary> summaries)
        {
            DeckSummaryDto[] dtos = new DeckSummaryDto[summaries.Count];
            for (int i = 0; i < summaries.Count; i++)
            {
                dtos[i] = ScheduleMappers.ToDto(summaries[i]);
            }

            DeckSummaryListDto wrapper = new DeckSummaryListDto { decks = dtos };
            WriteAtomic(_summariesPath, JsonUtility.ToJson(wrapper));
        }

        public IReadOnlyList<DeckSummary>? LoadDeckSummaries()
        {
            if (!File.Exists(_summariesPath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(_summariesPath);
                DeckSummaryListDto? wrapper = JsonUtility.FromJson<DeckSummaryListDto>(json);
                if (wrapper is null)
                {
                    return null;
                }

                return ScheduleMappers.FromDtos(wrapper.decks);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cache] Corrupt deck-summaries cache: {ex.Message}");
                return null;
            }
        }

        public void AppendPending(SessionResult result)
        {
            string ticks = _clock.UtcNow.Ticks.ToString("D19");
            string fileName = $"{ticks}-{result.SessionId}.json";
            string path = Path.Combine(_pendingDir, fileName);
            string json = JsonUtility.ToJson(ScheduleMappers.ToDto(result));
            WriteAtomic(path, json);
        }

        public void RemovePending(Guid sessionId)
        {
            if (!Directory.Exists(_pendingDir))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(_pendingDir, $"*-{sessionId}.json"))
            {
                File.Delete(file);
            }
        }

        public IReadOnlyList<SessionResult> LoadPending()
        {
            if (!Directory.Exists(_pendingDir))
            {
                return Array.Empty<SessionResult>();
            }

            IEnumerable<string> files = Directory.EnumerateFiles(_pendingDir, "*.json")
                .OrderBy(Path.GetFileName);

            List<SessionResult> results = new List<SessionResult>();
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    SessionResultDto dto = JsonUtility.FromJson<SessionResultDto>(json);
                    results.Add(ScheduleMappers.FromDto(dto));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Cache] Skipping corrupt pending file '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            return results;
        }

        private string SchedulePath(DeckId deckId)
        {
            return Path.Combine(_schedulesDir, $"{deckId.Value}.json");
        }

        private static void WriteAtomic(string finalPath, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            string tmp = finalPath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(finalPath))
            {
                File.Replace(tmp, finalPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmp, finalPath);
            }
        }
    }
}
