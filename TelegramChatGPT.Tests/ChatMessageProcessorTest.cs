using TelegramChatGPT.Implementation;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests
{
    public class ChatMessageProcessorTests
    {
        [Fact]
        public async Task HandleMessageCancellationTokenIsCancelledDoesNotProcessMessage()
        {
            var commandProcessorMock = new Mock<IChatCommandProcessor>();
            var chatMock = new Mock<IChat>();
            var messageMock = new Mock<IChatMessage>();
            var processor = new ChatMessageProcessor(commandProcessorMock.Object);

            var cancellationToken = new CancellationToken(true);

            await processor.HandleMessage(chatMock.Object, messageMock.Object, cancellationToken).ConfigureAwait(true);

            commandProcessorMock.Verify(cp => cp.ExecuteIfChatCommand(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            chatMock.Verify(c => c.DoResponseToMessage(It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageCommandExecutedStopsProcessing()
        {
            var commandProcessorMock = new Mock<IChatCommandProcessor>();
            commandProcessorMock.Setup(cp => cp.ExecuteIfChatCommand(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true);
            var chatMock = new Mock<IChat>();
            var messageMock = new Mock<IChatMessage>();
            var processor = new ChatMessageProcessor(commandProcessorMock.Object);

            await processor.HandleMessage(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            commandProcessorMock.Verify(cp => cp.ExecuteIfChatCommand(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            chatMock.Verify(c => c.DoResponseToMessage(It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageCommandNotExecutedInvokesDoResponseToMessage()
        {
            var commandProcessorMock = new Mock<IChatCommandProcessor>();
            commandProcessorMock.Setup(cp => cp.ExecuteIfChatCommand(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            var chatMock = new Mock<IChat>();
            var messageMock = new Mock<IChatMessage>();
            var processor = new ChatMessageProcessor(commandProcessorMock.Object);

            await processor.HandleMessage(chatMock.Object, messageMock.Object, CancellationToken.None).ConfigureAwait(true);

            commandProcessorMock.Verify(cp => cp.ExecuteIfChatCommand(It.IsAny<IChat>(), It.IsAny<IChatMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            chatMock.Verify(c => c.DoResponseToMessage(messageMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
