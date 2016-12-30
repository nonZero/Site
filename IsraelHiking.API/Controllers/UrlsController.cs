﻿using IsraelHiking.Common;
using IsraelHiking.DataAccessInterfaces;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using IsraelHiking.API.Services;
using Newtonsoft.Json;

namespace IsraelHiking.API.Controllers
{
    /// <summary>
    /// This controller handles the shared routes
    /// </summary>
    public class UrlsController : ApiController
    {
        private IIsraelHikingRepository _repository;
        private readonly IDataContainerConverterService _dataContainerConverterService;

        /// <summary>
        /// Controller's constructor
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="dataContainerConverterService"></param>
        public UrlsController(IIsraelHikingRepository repository, 
            IDataContainerConverterService dataContainerConverterService)
        {
            _repository = repository;
            _dataContainerConverterService = dataContainerConverterService;
        }

        /// <summary>
        /// Returns the data relevant to a given shared route
        /// </summary>
        /// <param name="id">The shared route ID</param>
        /// <param name="format">The format to convert to, default is <see cref="DataContainer"/>, but you can use "gpx", "csv" and all other formats that can be opened in this site</param>
        /// <returns>The shared route in the requested format</returns>
        // GET api/Urls/abc?format=gpx
        public async Task<IHttpActionResult> GetSiteUrl(string id, string format = "")
        {
            var siteUrl = await _repository.GetUrlById(id);
            if (siteUrl == null)
            {
                return BadRequest();
            }
            siteUrl.LastViewed = DateTime.Now;
            siteUrl.ViewsCount++;
            await _repository.Update(siteUrl);
            if (string.IsNullOrWhiteSpace(format))
            {
                return Ok(siteUrl);
            }
            var fileResponse = await GetUrlAsFile(id, format, siteUrl);
            return ResponseMessage(fileResponse);
        }

        private async Task<HttpResponseMessage> GetUrlAsFile(string id, string format, SiteUrl siteUrl)
        {
            var bytes = await _dataContainerConverterService.ToAnyFormat(JsonConvert.DeserializeObject<DataContainer>(siteUrl.JsonData), format);
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = id + "." + format
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/format");
            return result;
        }

        /// <summary>
        /// Get all shared routes for a specific user
        /// </summary>
        /// <returns>The user's shared routes</returns>
        // GET api/Urls
        [Authorize]
        public async Task<IHttpActionResult> GetSiteUrlForUser()
        {
            var siteUrls = await _repository.GetUrlsByUser(User.Identity.Name);
            return Ok(siteUrls);
        }

        /// <summary>
        /// Adds a shared route, user ID is optional
        /// </summary>
        /// <param name="siteUrl">The shared route's data</param>
        /// <returns>Whether the operation succeeded or not</returns>
        // POST api/urls
        public async Task<IHttpActionResult> PostSiteUrl(SiteUrl siteUrl)
        {
            if (string.IsNullOrWhiteSpace(siteUrl.OsmUserId) == false && siteUrl.OsmUserId != User.Identity.Name)
            {
                return BadRequest("You can't create a share as someone else!");
            }
            var random = new Random(Guid.NewGuid().GetHashCode());
            siteUrl.CreationDate = DateTime.Now;
            siteUrl.LastViewed = DateTime.Now;
            siteUrl.ViewsCount = 0;
            var id = GetRandomString(10, random);
            while (await _repository.GetUrlById(id) != null)
            {
                id = GetRandomString(10, random);
            }
            siteUrl.Id = id;
            await _repository.AddUrl(siteUrl);
            return Ok(siteUrl);
        }

        /// <summary>
        /// Update a shared route
        /// </summary>
        /// <param name="id">The shared route's ID</param>
        /// <param name="siteUrl">The new shared route data</param>
        /// <returns>Whether the operation succeeded or not</returns>
        // PUT api/urls/42
        [Authorize]
        public async Task<IHttpActionResult> PutSiteUrl(string id, SiteUrl siteUrl)
        {
            var siteUrlFromDatabase = await _repository.GetUrlById(id);
            if (siteUrlFromDatabase == null)
            {
                return NotFound();
            }
            if (siteUrlFromDatabase.OsmUserId != User.Identity.Name)
            {
                return BadRequest("You can't update someone else's share!");
            }
            siteUrlFromDatabase.Title = siteUrl.Title;
            siteUrlFromDatabase.Description = siteUrl.Description;
            await _repository.Update(siteUrlFromDatabase);
            return Ok(siteUrlFromDatabase);
        }

        // Delete delete/urls/abc
        /// <summary>
        /// Deletes the shared route.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<IHttpActionResult> DeleteSiteUrl(string id)
        {
            var siteUrlFromDatabase = await _repository.GetUrlById(id);
            if (siteUrlFromDatabase == null)
            {
                return NotFound();
            }
            if (siteUrlFromDatabase.OsmUserId != User.Identity.Name)
            {
                return BadRequest("You can't delete someone else's share!");
            }
            await _repository.Delete(siteUrlFromDatabase);
            return Ok();
        }

        private static string GetRandomString(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Follows dispose pattern
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repository.Dispose();
                _repository = null;
            }

            base.Dispose(disposing);
        }
    }
}
