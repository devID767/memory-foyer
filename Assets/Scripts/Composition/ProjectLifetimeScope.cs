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
            MessagePipeOptions options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<DeckSelectedEvent>(options);
            builder.RegisterMessageBroker<SessionStartedEvent>(options);
            builder.RegisterMessageBroker<CardReviewedEvent>(options);
            builder.RegisterMessageBroker<SessionFinishedEvent>(options);

            ServerConfigAsset? serverAsset = Resources.Load<ServerConfigAsset>("Config/ServerConfig");
            if (serverAsset == null)
            {
                throw new InvalidOperationException(
                    "ServerConfigAsset missing at Resources/Config/ServerConfig.asset");
            }

            ServerConfig serverConfig = serverAsset.ToConfig();
            builder.RegisterInstance<ServerConfig>(serverConfig);

            DeckAsset[] deckAssets = Resources.LoadAll<DeckAsset>("Decks");
            if (deckAssets.Length == 0)
            {
                throw new InvalidOperationException(
                    "No DeckAsset entries found under Resources/Decks/");
            }

            builder.RegisterInstance<IReadOnlyList<DeckAsset>>(deckAssets);

            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register<IDeckRepository, ScriptableObjectDeckRepository>(Lifetime.Singleton);
            builder.Register<IHttpClient, UnityWebRequestHttpClient>(Lifetime.Singleton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<IAnalyticsService, ConsoleAnalyticsService>(Lifetime.Singleton);
#else
            builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);
#endif

            // persistentDataPath is read here so Application stays UnityEngine-free.
            string cacheRoot = Path.Combine(Application.persistentDataPath, "ScheduleCache");
            builder.Register<IScheduleCache>(resolver =>
                new JsonFileScheduleCache(cacheRoot, resolver.Resolve<IClock>()),
                Lifetime.Singleton);

            // HttpScheduleStore stays concrete-only so it never satisfies IScheduleStore directly;
            // CachingScheduleStore owns the alias and stays reachable as a concrete type for DrainPendingAsync.
            builder.Register<HttpScheduleStore>(Lifetime.Singleton);
            builder.Register<CachingScheduleStore>(resolver =>
                new CachingScheduleStore(
                    inner:     resolver.Resolve<HttpScheduleStore>(),
                    cache:     resolver.Resolve<IScheduleCache>(),
                    analytics: resolver.Resolve<IAnalyticsService>()),
                Lifetime.Singleton)
                .As<IScheduleStore>()
                .AsSelf();

            builder.Register<IReviewSessionService, ReviewSessionService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CompositionSmokeTest>();
            builder.RegisterEntryPoint<PendingSessionDrainer>();
        }
    }
}
