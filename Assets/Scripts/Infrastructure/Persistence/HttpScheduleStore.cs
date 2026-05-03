using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Http;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Time;
using MemoryFoyer.Infrastructure.Dtos;

namespace MemoryFoyer.Infrastructure.Persistence
{
    public sealed class HttpScheduleStore : IScheduleStore
    {
        private readonly IHttpClient _http;
        private readonly IClock _clock;

        public HttpScheduleStore(IHttpClient http, IClock clock)
        {
            _http = http;
            _clock = clock;
        }

        public async UniTask<DeckSchedule> GetDeckScheduleAsync(DeckId deckId, CancellationToken ct = default)
        {
            string path = $"/decks/{Uri.EscapeDataString(deckId.Value)}/schedule";
            try
            {
                DeckScheduleDto dto = await _http.GetAsync<DeckScheduleDto>(path, ct);
                return ScheduleMappers.FromDto(dto, _clock, ScheduleSource.Server);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpTransportException ex)
            {
                throw new ScheduleStoreUnavailableException(
                    $"GET {path} failed: {ex.Message}", ex);
            }
            catch (HttpContractException ex)
            {
                throw new ScheduleStoreContractException(
                    $"GET {path} returned contract error: {ex.Message}", ex.StatusCode, ex);
            }
            catch (FormatException ex)
            {
                throw new ScheduleStoreContractException(
                    "malformed schedule payload", null, ex);
            }
        }

        public async UniTask<DeckSchedule> UploadSessionAsync(SessionResult result, CancellationToken ct = default)
        {
            const string path = "/sessions";
            SessionResultDto body = ScheduleMappers.ToDto(result);

            try
            {
                SessionUploadResponseDto resp = await _http.PostAsync<SessionResultDto, SessionUploadResponseDto>(path, body, ct);

                if (string.IsNullOrEmpty(resp.updatedSchedule.deckId))
                {
                    throw new ScheduleStoreContractException(
                        "POST /sessions: response missing updatedSchedule", 200);
                }

                return ScheduleMappers.FromDto(resp.updatedSchedule, _clock, ScheduleSource.Server);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpTransportException ex)
            {
                throw new ScheduleStoreUnavailableException(
                    $"POST {path} failed: {ex.Message}", ex);
            }
            catch (HttpContractException ex)
            {
                throw new ScheduleStoreContractException(
                    $"POST {path} returned contract error: {ex.Message}", ex.StatusCode, ex);
            }
            catch (FormatException ex)
            {
                throw new ScheduleStoreContractException(
                    "malformed schedule payload", null, ex);
            }
        }

        public async UniTask<bool> IsServerReachableAsync(CancellationToken ct = default)
        {
            try
            {
                await _http.GetAsync<HealthDto>("/health", ct);
                return true;
            }
            catch (HttpTransportException)
            {
                return false;
            }
        }
    }
}
