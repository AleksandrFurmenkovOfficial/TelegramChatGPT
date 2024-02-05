using TelegramChatGPT.Implementation.ChatMessageActions;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Tests
{
    public class ChatMessageActionProcessorTests
    {
        [Fact]
        public async Task HandleMessageActionExistingActionExecutesAction()
        {
            var actionMock = new Mock<IChatMessageAction>();
            actionMock.Setup(a => a.GetId).Returns(new ActionId("exist"));
            var actions = new List<IChatMessageAction> { actionMock.Object };

            var processor = new ChatMessageActionProcessor(actions);
            var chatMock = new Mock<IChat>();

            await processor.HandleMessageAction(chatMock.Object, new ActionParameters(new ActionId("exist"), "msg123"), CancellationToken.None).ConfigureAwait(true);

            actionMock.Verify(a => a.Run(It.IsAny<IChat>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleMessageActionNonExistingActionDoesNotExecuteAction()
        {
            var actionMock = new Mock<IChatMessageAction>();
            actionMock.Setup(a => a.GetId).Returns(new ActionId("exist"));
            var actions = new List<IChatMessageAction> { actionMock.Object };

            var processor = new ChatMessageActionProcessor(actions);
            var chatMock = new Mock<IChat>();

            await processor.HandleMessageAction(chatMock.Object, new ActionParameters(new ActionId("nonExist"), "msg123"), CancellationToken.None).ConfigureAwait(true);

            actionMock.Verify(a => a.Run(It.IsAny<IChat>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
