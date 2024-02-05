using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TelegramChatGPT.Implementation.ChatCommands;
using TelegramChatGPT.Implementation.ChatMessageActions;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal static class App
    {
        [RequiresDynamicCode(
            "Calls Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider()")]
        public static Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += AppLogger.GlobalExceptionHandler;

            var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                throw new InvalidOperationException("The environment variable 'OPENAI_API_KEY' is not set.");
            }

            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            if (string.IsNullOrEmpty(telegramBotKey))
            {
                throw new InvalidOperationException("The environment variable 'TELEGRAM_BOT_KEY' is not set.");
            }

            var adminUserId = Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_USER_ID") ?? "";

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, openAiApiKey, telegramBotKey, adminUserId);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var chatProcessor = serviceProvider.GetRequiredService<IChatProcessor>();
            return chatProcessor.Run();
        }

        private static void ConfigureServices(
            IServiceCollection services,
            string openAiApiKey,
            string telegramBotKey,
            string adminUserId)
        {
            services.AddSingleton(new ConcurrentDictionary<string, IAppVisitor>());
            services.AddSingleton(new ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>>());
            services.AddSingleton<IAdminChecker, AdminChecker>(_ => new AdminChecker(adminUserId));
            services.AddSingleton<IAiAgentFactory, AiAgentFactory>(_ => new AiAgentFactory(openAiApiKey,
                new OpenAiImagePainter(openAiApiKey),
                new OpenAiImageDescriptor(openAiApiKey)));
            services.AddSingleton<ITelegramBotSource, TelegramBotSource>(_ =>
                new TelegramBotSource(telegramBotKey));
            services.AddSingleton<IMessenger, Messenger>();

            services.AddSingleton<IChatModeLoader, ChatModeLoader>();
            services.AddSingleton<IChatFactory, ChatFactory>(provider =>
            {
                var chatModeLoader = provider.GetRequiredService<IChatModeLoader>();
                var aiAgentFactory = provider.GetRequiredService<IAiAgentFactory>();
                var messenger = provider.GetRequiredService<IMessenger>();
                var chatFactory = new ChatFactory(chatModeLoader, aiAgentFactory, messenger);
                return chatFactory;
            });

            services.AddSingleton<IChatCommandProcessor, ChatCommandProcessor>(provider =>
            {
                var visitors = provider.GetRequiredService<ConcurrentDictionary<string, IAppVisitor>>();
                var chatModeLoader = provider.GetRequiredService<IChatModeLoader>();
                var commands = new List<IChatCommand>
                {
                    new ReStart(), new ShowVisitors(visitors), new AddAccess(visitors), new DelAccess(visitors),
                    new SetCommonMode(chatModeLoader), new SetEnglishMode(chatModeLoader)
                };
                var adminChecker = provider.GetRequiredService<IAdminChecker>();
                var chatCommandProcessor = new ChatCommandProcessor(commands, adminChecker);
                return chatCommandProcessor;
            });

            services.AddSingleton<IChatMessageProcessor, ChatMessageProcessor>();
            services.AddSingleton<IChatMessageActionProcessor, ChatMessageActionProcessor>(_ =>
            {
                var actions = new List<IChatMessageAction>
                {
                    new CancelAction(), new StopAction(), new RegenerateAction(), new ContinueAction(),
                    new RetryAction()
                };
                return new ChatMessageActionProcessor(actions);
            });

            services.AddSingleton<IChatMessageConverter, ChatMessageConverter>(provider =>
            {
                var chatMessageConverter =
                    new ChatMessageConverter(telegramBotKey, provider.GetRequiredService<ITelegramBotSource>());
                return chatMessageConverter;
            });

            services.AddSingleton<IChatProcessor, ChatProcessor>(provider =>
            {
                var chatProcessor = new ChatProcessor(
                    provider.GetRequiredService<ConcurrentDictionary<string, IAppVisitor>>(),
                    provider.GetRequiredService<ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>>>(),
                    provider.GetRequiredService<IAdminChecker>(),
                    provider.GetRequiredService<IChatFactory>(),
                    provider.GetRequiredService<IChatMessageProcessor>(),
                    provider.GetRequiredService<IChatMessageActionProcessor>(),
                    provider.GetRequiredService<IChatMessageConverter>(),
                    provider.GetRequiredService<ITelegramBotSource>());
                return chatProcessor;
            });
        }
    }
}