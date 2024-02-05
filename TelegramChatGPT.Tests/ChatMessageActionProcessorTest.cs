using TelegramChatGPT.Implementation.ChatCommands;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests
{
    public class ChatCommandProcessorTests
    {
        [Fact]
        public async Task ExecuteIfChatCommandWhenCommandExistsExecutesCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock);
            messageMock.SetupProperty(x => x.Content, "/test");

            var result = await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            Assert.True(result);
            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteIfChatCommandWhenNoCommandInMessageDoesNotExecuteAnyCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock);
            messageMock.SetupProperty(x => x.Content, "This is a regular message without a command");

            var result = await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            Assert.False(result);
            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteIfChatCommandWhenCommandIsAnywhereInMessageExecutesCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock);
            messageMock.SetupProperty(x => x.Content, "This message includes a /test command somewhere. Needed to allow wrap text content in JSON etc.");

            var result = await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            Assert.True(result);
            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteIfChatCommandWhenAdminCommandAndUserIsNotAdminDoesNotExecuteCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock, adminCommand: true, isAdmin: false);
            messageMock.SetupProperty(x => x.Content, "/test");

            var result = await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            Assert.False(result);
            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteIfChatCommandWhenAdminCommandAndUserIsAdminDoesExecuteCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock, adminCommand: true, isAdmin: true);
            messageMock.SetupProperty(x => x.Content, "/test");

            var result = await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            Assert.True(result);
            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteIfChatCommandPassesCorrectParametersToCommand()
        {
            var processor = SetupProcessor(out var commandMock, out var chatMock, out var messageMock);
            var cancellationToken = new CancellationToken(canceled: false);

            string expectedContent = "/test some arguments";
            messageMock.SetupProperty(x => x.Content, expectedContent);

            commandMock.Setup(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()))
                       .Callback<IChat, IChatMessage, CancellationToken>((chat, message, token) =>
                       {
                           Assert.Same(chatMock.Object, chat);
                           Assert.Equal(expectedContent["/test".Length..], message.Content);
                       })
                       .Returns(Task.CompletedTask);

            await processor.ExecuteIfChatCommand(chatMock.Object, messageMock.Object, cancellationToken).ConfigureAwait(true);

            commandMock.Verify(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), cancellationToken), Times.Once);
        }

        private static ChatCommandProcessor SetupProcessor(out Mock<IChatCommand> commandMock, out Mock<IChat> chatMock, out Mock<IChatMessage> messageMock, bool adminCommand = false, bool isAdmin = false)
        {
            commandMock = new Mock<IChatCommand>();
            commandMock.SetupGet(x => x.Name).Returns("test");
            commandMock.Setup(x => x.IsAdminOnlyCommand).Returns(adminCommand);
            commandMock.Setup(x => x.Execute(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var adminCheckerMock = new Mock<IAdminChecker>();
            adminCheckerMock.Setup(x => x.IsAdmin(It.IsAny<string>())).Returns(isAdmin);

            var commands = new List<IChatCommand> { commandMock.Object };
            var processor = new ChatCommandProcessor(commands, adminCheckerMock.Object);

            chatMock = new Mock<IChat>();
            chatMock.SetupGet(x => x.Id).Returns("123");

            messageMock = new Mock<IChatMessage>();

            return processor;
        }
    }
}
