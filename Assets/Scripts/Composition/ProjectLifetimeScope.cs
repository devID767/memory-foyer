using System;
using System.Collections.Generic;
using System.IO;
using MessagePipe;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Application.Configuration;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Http;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Application.Sessions;
using MemoryFoyer.Domain.Time;
using MemoryFoyer.Infrastructure.Analytics;
using MemoryFoyer.Infrastructure.Http;
using MemoryFoyer.Infrastructure.Persistence;
using MemoryFoyer.Infrastructure.Repositories;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 1. MessagePipe — register first; brokers depend on the options object
            MessagePipeOptions options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<DeckSelectedEvent>(options);
            builder.RegisterMessageBroker<SessionStartedEvent>(options);
            builder.RegisterMessageBroker<CardReviewedEvent>(options);
            builder.RegisterMessageBroker<SessionFinishedEvent>(options);

            // 2. ServerConfig — load SO from Resources, fail loud if missing
            ServerConfigAsset? serverAsset = Resources.Load<ServerConfigAsset>("Config/ServerConfig");
            if (serverAsset == null)
            {
                throw new InvalidOperationException(
                    "ServerConfigAsset missing at Resources/Config/ServerConfig.asset");
            }

            ServerConfig serverConfig = serverAsset.ToConfig();
            builder.RegisterInstance<ServerConfig>(serverConfig);

            // 3. DeckAssets — load all from Resources, fail loud if empty
            DeckAsset[] deckAssets = Resources.LoadAll<DeckAsset>("Decks");
            if (deckAssets.Length == 0)
            {
                throw new InvalidOperationException(
                    "No DeckAsset entries found under Resources/Decks/");
            }

            builder.RegisterInstance<IReadOnlyList<DeckAsset>>(deckAssets);

            // 4. Domain primitives
            builder.Register<IClock, SystemClock>(Lifetime.Singleton);

            // 5. Repositories
            builder.Register<IDeckRepository, ScriptableObjectDeckRepository>(Lifetime.Singleton);

            // 6. HTTP
            builder.Register<IHttpClient, UnityWebRequestHttpClient>(Lifetime.Singleton);

            // 7. Analytics — conditional dev/release
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<IAnalyticsService, ConsoleAnalyticsService>(Lifetime.Singleton);
#else
            builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);
#endif

            // 8. Schedule cache — root path injected from Composition (Application can't see persistentDataPath)
            string cacheRoot = Path.Combine(Application.persistentDataPath, "ScheduleCache");
            builder.Register<IScheduleCache>(resolver =>
                new JsonFileScheduleCache(cacheRoot, resolver.Resolve<IClock>()),
                Lifetime.Singleton);

            // 9. Schedule store triple
            //    HttpScheduleStore registered as concrete-only — invisible to anyone resolving IScheduleStore.
            //    CachingScheduleStore registered as concrete + aliased to IScheduleStore via .As<T>(),
            //    so PendingSessionDrainer can resolve the concrete type for DrainPendingAsync().
            builder.Register<HttpScheduleStore>(Lifetime.Singleton);
            builder.Register<CachingScheduleStore>(resolver =>
                new CachingScheduleStore(
                    inner:     resolver.Resolve<HttpScheduleStore>(),
                    cache:     resolver.Resolve<IScheduleCache>(),
                    analytics: resolver.Resolve<IAnalyticsService>()),
                Lifetime.Singleton)
                .As<IScheduleStore>();

            // 10. Session service
            builder.Register<IReviewSessionService, ReviewSessionService>(Lifetime.Singleton);

            // 11. Entry points
            builder.RegisterEntryPoint<CompositionSmokeTest>();
            builder.RegisterEntryPoint<PendingSessionDrainer>();
        }
    }
}
