﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GogoKit.Helpers;
using GogoKit.Models;
using GogoKit.Resources;

namespace GogoKit.Http
{
    public class ApiConnection : IApiConnection
    {
        private const string HalJsonMediaType = "application/hal+json";

        private readonly IConnection _connection;
        private readonly ILinkResolver _linkResolver;

        public ApiConnection(IConnection connection)
            : this(connection, new LinkResolver())
        {
        }

        public ApiConnection(IConnection connection, ILinkResolver linkResolver)
        {
            Requires.ArgumentNotNull(connection, "connection");

            _connection = connection;
            _linkResolver = linkResolver;
        }

        public IConnection Connection
        {
            get { return _connection; }
        }

        public async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(
            Link link,
            IDictionary<string, string> parameters) where T : Resource
        {
            var currentLink = link;
            var currentParameters = parameters;
            var items = new List<T>();
            var hasAnotherPage = true;
            while (hasAnotherPage)
            {
                var currentPage = await SendRequestAsyncAndGetBody<PagedResource<T>>(
                                            HttpMethod.Get,
                                            currentLink,
                                            currentParameters).ConfigureAwait(false);

                items.AddRange(currentPage.Items);

                // Stop passing parameters on subsequent calls since the "next" links
                // will already be assembled with all the parameters needed
                currentParameters = null;
                hasAnotherPage = currentPage.Links.TryGetLink("next", out currentLink);
            }

            return items;
        }

        public Task<T> GetAsync<T>(Link link, IDictionary<string, string> parameters)
        {
            return SendRequestAsyncAndGetBody<T>(HttpMethod.Get, link, parameters);
        }

        public Task<T> PostAsync<T>(Link link, IDictionary<string, string> parameters, object body)
        {
            return SendRequestAsyncAndGetBody<T>(HttpMethod.Post, link, parameters, data: body, contentType: HalJsonMediaType);
        }

        public Task<T> PatchAsync<T>(Link link, IDictionary<string, string> parameters, object body)
        {
            return SendRequestAsyncAndGetBody<T>(new HttpMethod("Patch"), link, parameters, data: body, contentType: HalJsonMediaType);
        }

        public Task<T> PutAsync<T>(Link link, IDictionary<string, string> parameters, object body)
        {
            return SendRequestAsyncAndGetBody<T>(HttpMethod.Put, link, parameters, data: body, contentType: HalJsonMediaType);
        }

        public async Task<IApiResponse> DeleteAsync(Link link, IDictionary<string, string> parameters)
        {
            return await SendRequestAsync<object>(HttpMethod.Delete, link, parameters);
        }

        private async Task<T> SendRequestAsyncAndGetBody<T>(
            HttpMethod method,
            Link link,
            IDictionary<string, string> parameters,
            string accept = HalJsonMediaType,
            object data = null,
            string contentType = null)
        {
            var response = await SendRequestAsync<T>(method, link, parameters, accept, data, contentType).ConfigureAwait(false);
            return response.BodyAsObject;
        }

        private async Task<IApiResponse<T>> SendRequestAsync<T>(
            HttpMethod method,
            Link link,
            IDictionary<string, string> parameters,
            string accept = HalJsonMediaType,
            object data = null,
            string contentType = null)
        {
            Requires.ArgumentNotNull(method, "method");
            Requires.ArgumentNotNull(link, "link");

            var response = await _connection.SendRequestAsync<T>(
                                    _linkResolver.ResolveLink(link, parameters),
                                    method,
                                    accept,
                                    data,
                                    contentType).ConfigureAwait(false);
            return response;
        }
    }
}
