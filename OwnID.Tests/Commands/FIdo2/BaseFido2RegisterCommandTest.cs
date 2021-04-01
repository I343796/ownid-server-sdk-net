using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using Fido2NetLib;
using Moq;
using OwnID.Commands.Fido2;
using OwnID.Extensibility.Cache;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Exceptions;
using OwnID.Extensibility.Flow.Contracts;
using OwnID.Extensibility.Providers;
using OwnID.Flow;
using OwnID.Services;
using Xunit;

namespace OwnID.Tests.Commands.FIdo2
{
    public class BaseFido2RegisterCommandTest
    {
        private readonly Mock<ICacheItemRepository> _cacheItemRepository;
        private readonly CultureInfo _culture;

        private readonly StepType _currentStepType;
        private readonly DateTime _date;
        private readonly Mock<IEncodingService> _encodingService;

        private readonly Mock<IFido2> _fido2;
        private readonly Mock<IFido2Configuration> _fido2Configuration;
        private readonly Fixture _fixture;
        private readonly Mock<IIdentitiesProvider> _identitiesProvider;
        private readonly bool _isStateless;
        private readonly Mock<IOwnIdCoreConfiguration> _ownIdCoreConfiguration;
        private readonly CacheItem _relatedItem;

        private readonly RequestIdentity _requestIdentity;

        private readonly Fido2RegisterCommand _sut;

        private readonly string _userId;


        public BaseFido2RegisterCommandTest()
        {
            _fixture = new Fixture();
            //_fixture.Register(() => new Fido2Configuration());

            // Dependencies
            _fido2 = new Mock<IFido2>();
            _cacheItemRepository = new Mock<ICacheItemRepository>();
            _ownIdCoreConfiguration = new Mock<IOwnIdCoreConfiguration>();
            _fido2Configuration = new Mock<IFido2Configuration>();
            _ownIdCoreConfiguration.Setup(c => c.Fido2).Returns(_fido2Configuration.Object);

            _encodingService = new Mock<IEncodingService>();

            _userId = _fixture.Freeze<string>();
            _identitiesProvider = new Mock<IIdentitiesProvider>();
            _identitiesProvider.Setup(x => x.GenerateUserId()).Returns(_userId);

            // Parameters for ExecuteAsync
            _currentStepType = _fixture.Freeze<StepType>();
            _isStateless = _fixture.Freeze<bool>();
            _relatedItem = _fixture.Freeze<CacheItem>();

            // Parameters for creating CommandInput
            _requestIdentity = _fixture.Freeze<RequestIdentity>();
            _culture = _fixture.Freeze<CultureInfo>();
            _date = _fixture.Freeze<DateTime>();

            _sut = new Fido2RegisterCommand(_fido2.Object, _cacheItemRepository.Object, _ownIdCoreConfiguration.Object,
                _identitiesProvider.Object,
                _encodingService.Object);
        }

        private TransitionInput<string> GetInput(string data)
        {
            return new(_requestIdentity, _culture, data, _date);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(JsonException), "invalid JSON")]
        [InlineData(typeof(CommandValidationException),
            "{\"clientDataJSON\": \"\", \"attestationObject\": \"AttestationObject\"}")]
        [InlineData(typeof(CommandValidationException),
            "{\"clientDataJSON\": \"clientDataJSON\", \"attestationObject\": \"\"}")]
        public async Task FailsWithInvalidFido2Request(Type expectedException, string inputJson)
        {
            var input = GetInput(inputJson);

            await Assert.ThrowsAsync(expectedException,
                async () => { await _sut.ExecuteAsync(input.Data, _relatedItem); });
        }

        [Fact]
        public async Task FailIfNoResultFromFido2Validation()
        {
            var input = GetInput(
                "{\"clientDataJSON\": \"clientDataJSON\", \"attestationObject\": \"AttestationObject\"}");

            await Assert.ThrowsAsync<InternalLogicException>(async () =>
            {
                await _sut.ExecuteAsync(input.Data, _relatedItem);
            });
        }

        // [Fact]
        // public async Task Pass()
        // {
        //     var input = GetInput(
        //         "{\"clientDataJSON\": \"clientDataJSON\", \"attestationObject\": \"AttestationObject\"}");
        //
        //     var attRes = new Mock<AttestationVerificationSuccess>();
        //
        //     //
        //     // TODO: Setup .Result with some public key
        //     // 
        //     var fido2Result = new Mock<Fido2.CredentialMakeResult>();
        //     
        //     fido2Result.Setup(x => x.Result).Returns(attRes.Object);
        //
        //     _fido2.Setup(x => x.MakeNewCredentialAsync(It.IsAny<AuthenticatorAttestationRawResponse>(),
        //         It.IsAny<CredentialCreateOptions>(), It.IsAny<IsCredentialIdUniqueToUserAsyncDelegate>(),
        //         It.IsAny<byte[]>())).ReturnsAsync(fido2Result.Object);
        //
        //     await Assert.ThrowsAsync<InternalLogicException>(async () =>
        //     {
        //         await _sut.ExecuteAsync(input.Data, _relatedItem);
        //     });
        // }
    }
}