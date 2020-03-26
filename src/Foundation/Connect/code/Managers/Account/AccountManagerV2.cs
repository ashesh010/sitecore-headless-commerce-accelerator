﻿//    Copyright 2020 EPAM Systems, Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace Wooli.Foundation.Connect.Managers.Account
{
    using System;

    using Base.Models.Logging;
    using Base.Services.Logging;

    using DependencyInjection;

    using Providers.Contracts;

    using Sitecore.Commerce.Entities.Customers;
    using Sitecore.Commerce.Services;
    using Sitecore.Commerce.Services.Customers;
    using Sitecore.Diagnostics;

    [Service(typeof(IAccountManagerV2), Lifetime = Lifetime.Singleton)]
    public class AccountManagerV2 : IAccountManagerV2
    {
        private readonly CustomerServiceProvider customerServiceProvider;
        private readonly ILogService<CommonLog> logService;

        public AccountManagerV2(IConnectServiceProvider connectServiceProvider, ILogService<CommonLog> logService)
        {
            Assert.ArgumentNotNull(connectServiceProvider, nameof(connectServiceProvider));
            Assert.ArgumentNotNull(logService, nameof(logService));

            this.customerServiceProvider = connectServiceProvider.GetCustomerServiceProvider();
            this.logService = logService;
        }

        public GetPartiesResult GetCustomerParties(string contactId)
        {
            Assert.ArgumentNotNullOrEmpty(contactId, nameof(contactId));

            var getUserResult = this.GetUser(contactId);

            if (!getUserResult.Success || getUserResult.CommerceUser == null)
            {
                var result = new GetPartiesResult
                {
                    Success = false
                };

                foreach (var message in getUserResult.SystemMessages)
                {
                    result.SystemMessages.Add(message);
                }

                return result;
            }

            var customer = new CommerceCustomer
            {
                ExternalId = getUserResult.CommerceUser.ExternalId
            };

            return this.GetParties(customer);
        }
        
        public GetPartiesResult GetParties(CommerceCustomer customer)
        {
            Assert.ArgumentNotNull(customer, nameof(customer));

            return this.Execute(new GetPartiesRequest(customer), this.customerServiceProvider.GetParties);
        }

        public GetUserResult GetUser(string userName)
        {
            Assert.ArgumentNotNullOrEmpty(userName, nameof(userName));

            return this.Execute(new GetUserRequest(userName), this.customerServiceProvider.GetUser);
        }

        //TODO: Remove Duplication of execute method from managers
        private TResult Execute<TRequest, TResult>(TRequest request, Func<TRequest, TResult> action)
            where TRequest : ServiceProviderRequest
            where TResult : ServiceProviderResult
        {
            var providerResult = action.Invoke(request);

            if (!providerResult.Success)
            {
                foreach (var systemMessage in providerResult.SystemMessages)
                {
                    this.logService.Error(systemMessage.Message);
                }
            }

            return providerResult;
        }
    }
}