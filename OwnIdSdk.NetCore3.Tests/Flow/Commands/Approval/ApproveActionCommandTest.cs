using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OwnIdSdk.NetCore3.Extensibility.Exceptions;
using OwnIdSdk.NetCore3.Extensibility.Flow.Contracts;
using OwnIdSdk.NetCore3.Flow.Commands.Approval;
using OwnIdSdk.NetCore3.Services;
using OwnIdSdk.NetCore3.Tests.TestUtils;
using Xunit;

namespace OwnIdSdk.NetCore3.Tests.Flow.Commands.Approval
{
    public class ApproveActionCommandTest
    {
        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_Fail_EmptyContext(Mock<ICacheItemService> cacheService,
            ApproveActionRequest request)
        {
            const string errorMessage = "Context and nonce are required";
            request.Context = null;
            var command = new ApproveActionCommand(cacheService.Object);
            await command.Invoking(x => x.ExecuteAsync(request)).Should().ThrowAsync<CommandValidationException>()
                .WithMessage(errorMessage);
            request.Context = string.Empty;
            await command.Invoking(x => x.ExecuteAsync(request)).Should().ThrowAsync<CommandValidationException>()
                .WithMessage(errorMessage);
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_Fail_EmptyNonce(Mock<ICacheItemService> cacheService,
            ApproveActionRequest request)
        {
            const string errorMessage = "Context and nonce are required";
            request.Nonce = null;
            var command = new ApproveActionCommand(cacheService.Object);
            await command.Invoking(x => x.ExecuteAsync(request)).Should().ThrowAsync<CommandValidationException>()
                .WithMessage(errorMessage);
            request.Nonce = string.Empty;
            await command.Invoking(x => x.ExecuteAsync(request)).Should().ThrowAsync<CommandValidationException>()
                .WithMessage(errorMessage);
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_Success(Mock<ICacheItemService> cacheService, ApproveActionRequest request)
        {
            var command = new ApproveActionCommand(cacheService.Object);
            await command.ExecuteAsync(request);
            cacheService.Verify(x => x.SetApprovalResolutionAsync(request.Context, request.Nonce, request.IsApproved),
                Times.Once);
        }
    }
}