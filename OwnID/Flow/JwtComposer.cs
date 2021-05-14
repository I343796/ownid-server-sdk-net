using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OwnID.Cryptography;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Flow.Contracts.Internal;
using OwnID.Extensibility.Services;
using OwnID.Extensions;
using OwnID.Flow.Interfaces;
using OwnID.Flow.ResultActions;

namespace OwnID.Flow
{
    /// <inheritdoc cref="IJwtComposer" />
    public class JwtComposer : IJwtComposer
    {
        private readonly IJwtService _jwtService;
        private readonly ILocalizationService _localizationService;
        private readonly IOwnIdCoreConfiguration _ownIdCoreConfiguration;

        /// <summary>
        /// </summary>
        /// <param name="ownIdCoreConfiguration">Core configuration to be used</param>
        /// <param name="jwtService">Service for generating JWT</param>
        /// <param name="localizationService">Optional(only if localization is needed). Localization service</param>
        public JwtComposer([NotNull] IOwnIdCoreConfiguration ownIdCoreConfiguration,
            [NotNull] IJwtService jwtService, ILocalizationService localizationService)
        {
            _jwtService = jwtService;
            _localizationService = localizationService;
            _ownIdCoreConfiguration = ownIdCoreConfiguration;
        }

        public string GenerateProfileConfigJwt(BaseJwtComposeInfo info, string did)
        {
            var data = GetFieldsConfigDictionary(did);

            if (info.IncludeRequester)
            {
                var (key, value) = GetRequester();
                data[key] = value;
            }

            var fields = GetBaseFlowFieldsDictionary(info, data);

            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        public string GeneratePinStepJwt(BaseJwtComposeInfo info, string pin)
        {
            var requester = GetRequester();

            var data = new Dictionary<string, object>
            {
                {"pin", pin},
                {requester.Key, requester.Value}
            };

            if (!string.IsNullOrEmpty(info.EncKey)) data.Add("encKey", info.EncKey);
            if (!string.IsNullOrEmpty(info.EncVector)) data.Add("encVector", info.EncVector);

            if (info.IncludeFido2FallbackBehavior && _ownIdCoreConfiguration.TFAEnabled)
                data.Add("fido2FallbackBehavior", _ownIdCoreConfiguration.Fido2FallbackBehavior.ToString().ToLower());

            data.Add("canBeRecovered", info.CanBeRecovered);

            var fields = GetBaseFlowFieldsDictionary(info, data);
            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        public string GenerateFinalStepJwt(BaseJwtComposeInfo info)
        {
            var fields = GetBaseFlowFieldsDictionary(info);
            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        public string GenerateBaseStepJwt(BaseJwtComposeInfo info, string did = null)
        {
            var data = new Dictionary<string, object>
            {
                {"requestedFields", new object[0]}
            };

            if (!string.IsNullOrEmpty(did))
            {
                var (didKey, didValue) = GetDid(did);
                data.Add(didKey, didValue);
            }

            if (info.IncludeRequester)
            {
                var (reqKey, reqValue) = GetRequester();
                data.Add(reqKey, reqValue);
            }

            if (!string.IsNullOrEmpty(info.EncKey)) data.Add("encKey", info.EncKey);
            if (!string.IsNullOrEmpty(info.EncVector)) data.Add("encVector", info.EncVector);

            if (info.IncludeFido2FallbackBehavior && _ownIdCoreConfiguration.TFAEnabled)
                data.Add("fido2FallbackBehavior", _ownIdCoreConfiguration.Fido2FallbackBehavior.ToString().ToLower());

            data.Add("canBeRecovered", info.CanBeRecovered);

            var fields = GetBaseFlowFieldsDictionary(info, data);

            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        public string GenerateRecoveryDataJwt(BaseJwtComposeInfo info, ConnectionRecoveryResult<object> data)
        {
            var dataDict = new Dictionary<string, object>
            {
                {"encKey", info.EncKey},
                {"encVector", info.EncVector}
            };

            if (data != null)
            {
                var (didKey, didValue) = GetDid(data.DID);
                dataDict = new Dictionary<string, object>
                {
                    {"profile", data.UserProfile},
                    {didKey, didValue},
                    {"pubKey", data.PublicKey},
                    {"recoveryData", data.RecoveryData}
                };
            }

            var fields = GetBaseFlowFieldsDictionary(info, dataDict);
            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        public string GenerateDataStepJwt<T>(BaseJwtComposeInfo info, T data) where T : class
        {
            var fields = GetBaseFlowFieldsDictionary(info, data);
            return _jwtService.GenerateDataJwt(fields, info.ClientTime);
        }

        private Dictionary<string, object> GetBaseFlowFieldsDictionary(BaseJwtComposeInfo info, object data = null)
        {
            var stepDict = GetStepBehavior(info.Behavior);

            var fields = new Dictionary<string, object>
            {
                {"jti", info.Context},
                {"locale", info.Locale},
                {"isDesktop", info.IsDesktop},
                {"nextStep", stepDict}
            };

            if (info.CookiesSet) fields.Add("cookiesSet", true);
            
            if (data != null)
                fields.Add("data", data);

            return fields;
        }

        private Dictionary<string, object> GetStepBehavior(FrontendBehavior nextFrontendBehavior)
        {
            if (nextFrontendBehavior.Error != null && nextFrontendBehavior.Type != StepType.Error)
                throw new ArgumentException(
                    $"{nameof(nextFrontendBehavior.Error)} can be supplied only if {nameof(nextFrontendBehavior.Type)} is '{nameof(StepType.Error)}'",
                    nameof(nextFrontendBehavior));

            if (nextFrontendBehavior.Error == null && nextFrontendBehavior.Type == StepType.Error)
                throw new ArgumentException(
                    $"{nameof(nextFrontendBehavior.Error)} must be provided if {nameof(nextFrontendBehavior.Type)} is '{nameof(StepType.Error)}'",
                    nameof(nextFrontendBehavior));

            var stepType = nextFrontendBehavior.Type.ToString();
            var actionType = nextFrontendBehavior.ActionType.ToString();
            var stepDict = new Dictionary<string, object>
            {
                {"type", stepType.First().ToString().ToLowerInvariant() + stepType.Substring(1)},
                {"actionType", actionType.First().ToString().ToLowerInvariant() + actionType.Substring(1)}
            };

            if (nextFrontendBehavior.Error != null)
                stepDict.Add("error", nextFrontendBehavior.Error.ToString().ToCamelCase());
            else
                stepDict.Add("challengeType", nextFrontendBehavior.ChallengeType.ToString().ToLowerInvariant());

            if (nextFrontendBehavior.Polling != null)
                stepDict.Add("polling", new
                {
                    url = nextFrontendBehavior.Polling.Url.ToString(),
                    method = nextFrontendBehavior.Polling.Method,
                    interval = nextFrontendBehavior.Polling.Interval
                });

            if (nextFrontendBehavior.Callback != null)
                stepDict.Add("callback", new
                {
                    url = nextFrontendBehavior.Callback.Url,
                    method = nextFrontendBehavior.Callback.Method
                });

            if (nextFrontendBehavior.NextBehavior != null)
                stepDict.Add("nextBehavior", GetStepBehavior(nextFrontendBehavior.NextBehavior));

            if (nextFrontendBehavior.AlternativeBehavior != null)
                stepDict.Add("alternativeBehavior", GetStepBehavior(nextFrontendBehavior.AlternativeBehavior));

            return stepDict;
        }

        private Dictionary<string, object> GetFieldsConfigDictionary(string did)
        {
            var dataFields = new Dictionary<string, object>
            {
                {
                    // TODO : PROFILE
                    "requestedFields", _ownIdCoreConfiguration.ProfileConfiguration.ProfileFieldMetadata.Select(x =>
                    {
                        var label = Localize(x.Label);

                        return new
                        {
                            type = x.Type,
                            key = x.Key,
                            label,
                            placeholder = Localize(x.Placeholder),
                            validators = x.Validators.Select(v => new
                            {
                                type = v.Type,
                                errorMessage =
                                    v.NeedsInternalLocalization
                                        ? v.FormatErrorMessage(label, Localize(v.ErrorKey))
                                        : v.FormatErrorMessage(label),
                                parameters = v.Parameters
                            })
                        };
                    })
                }
            };

            var (didKey, didValue) = GetDid(did);
            dataFields.Add(didKey, didValue);

            return dataFields;
        }

        private KeyValuePair<string, string> GetDid(string did)
        {
            return new("did", did);
        }

        private KeyValuePair<string, object> GetRequester()
        {
            return new("requester", new
            {
                did = _ownIdCoreConfiguration.DID,
                pubKey = RsaHelper.ExportPublicKeyToPkcsFormattedString(_ownIdCoreConfiguration
                    .JwtSignCredentials),
                name = Localize(_ownIdCoreConfiguration.Name),
                icon = _ownIdCoreConfiguration.Icon,
                description = Localize(_ownIdCoreConfiguration.Description),
                overwriteFields = _ownIdCoreConfiguration.OverwriteFields
            });
        }

        private string Localize(string key)
        {
            return _localizationService?.GetLocalizedString(key) ?? key;
        }
    }
}