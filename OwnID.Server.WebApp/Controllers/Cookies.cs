using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OwnID.Extensibility.Flow.Contracts.Cookies;
using OwnID.Server.WebApp.Models;

namespace OwnID.Server.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class Cookies : ControllerBase
    {
        private readonly Regex _connectionIdRegex;
        private readonly Regex _requesterRegex;
        private readonly WebAppOptions _webAppOptions;

        public Cookies(IOptions<WebAppOptions> webAppOptions)
        {
            _webAppOptions = webAppOptions.Value;
            _connectionIdRegex = new Regex(string.Format(CookieNameTemplates.Encryption, "(.*)"),
                RegexOptions.Compiled);
            _requesterRegex = new Regex("[()<>@,;:\\/\"[\\]?={}]+", RegexOptions.Compiled);
        }

        /// <summary>
        ///     Add new HttpOnly cookie
        /// </summary>
        /// <param name="cookie">Cookie name and value</param>
        /// <returns></returns>
        /// <response code="204">Cookie has been set successfully</response>
        /// <response code="415">Cookie value is missed or in the wrong format</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [HttpPut]
        public IActionResult Put(Cookie cookie)
        {
            Response.Cookies.Append(cookie.Key, cookie.Value,
                GetCookieOptions(DateTime.Now.AddDays(_webAppOptions.CookieExpiration)));

            return new NoContentResult();
        }

        /// <summary>
        ///     Get cookie
        /// </summary>
        /// <param name="name">cookie name</param>
        /// <returns>cookie value if any has been found, otherwise 404</returns>
        /// <response code="200">Returns cookie value</response>
        /// <response code="404">Cookie not found</response>
        [HttpGet]
        [Route("{name}")]
        public IActionResult Get(string name)
        {
            return new JsonResult(new Cookie
            {
                Key = name,
                Value = Request.Cookies[name]
            });
        }

        /// <summary>
        ///     Remove cookie
        /// </summary>
        /// <param name="name">cookie name</param>
        /// <response code="204">Success completion</response>
        [HttpDelete]
        [Route("{name}")]
        public IActionResult Delete(string name)
        {
            if (Request.Cookies.ContainsKey(name))
                Response.Cookies.Delete(name, GetCookieOptions());

            return NoContent();
        }

        /// <summary>
        ///     Remove passcode and connection cookies
        /// </summary>
        /// <param name="name">cookie name</param>
        /// <response code="204">Success completion</response>
        [HttpDelete]
        [Route("passcode/{name}")]
        public IActionResult ResetPasscode(string name)
        {
            var cookiesOptions = new CookieOptions
            {
                Secure = !_webAppOptions.IsDevEnvironment,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };

            foreach (var cookie in Request.Cookies)
            {
                if (!cookie.Value.Contains(
                    $"{CookieValuesConstants.SubValueSeparator}{CookieValuesConstants.PasscodeCookieType}{CookieValuesConstants.SubValueSeparator}")
                )
                    continue;

                var connectionId = _connectionIdRegex.Match(cookie.Key).Groups[1].Captures.FirstOrDefault()?.Value;

                if (string.IsNullOrEmpty(connectionId))
                    continue;

                Response.Cookies.Delete(string.Format(CookieNameTemplates.Encryption, connectionId),
                    cookiesOptions);
                Response.Cookies.Delete(string.Format(CookieNameTemplates.Recovery, connectionId),
                    cookiesOptions);
            }

            return Delete(name);
        }

        [HttpPost]
        [Route("conversion")]
        public IActionResult ConvertCookies(ConvertCookiesRequest request)
        {
            var cookiesOptions = new CookieOptions
            {
                Secure = !_webAppOptions.IsDevEnvironment,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Domain = !_webAppOptions.IsDevEnvironment ? ".ownid.com" : "localhost",
                Expires = DateTimeOffset.Now.AddYears(5)
            };
            
            var cookieName = _requesterRegex.Replace(request.RequesterId, "-");

            foreach (var cookie in Request.Cookies)
            {
                if (cookie.Key.EndsWith(cookieName))
                    Response.Cookies.Append(cookie.Key, cookie.Value, cookiesOptions);
            }

            return Ok();
        }

        private CookieOptions GetCookieOptions(DateTime? expireAt = null)
        {
            var options = new CookieOptions
            {
                Secure = !_webAppOptions.IsDevEnvironment,
                Path = "/cookies",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            };

            if (expireAt.HasValue)
                options.Expires = expireAt;

            return options;
        }
    }
}