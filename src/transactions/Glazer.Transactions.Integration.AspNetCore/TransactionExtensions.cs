using Glazer.Transactions.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glazer.Transactions.Integration.AspNetCore
{
    public static class TransactionExtensions
    {
        /// <summary>
        /// Set the transaction sets to service collection.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IServiceCollection SetTransactionSets(this IServiceCollection Services, Func<IServiceProvider, ITransactionSets> Factory)
            => Services.AddSingleton(Services => Factory(Services));

        /// <summary>
        /// Get the transaction set instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static ITransactionSets GetTransactionSets(this IServiceProvider Services) => Services.GetRequiredService<ITransactionSets>();

        /// <summary>
        /// Add Transaction Set API sets to MVC builder.
        /// </summary>
        /// <param name="MvcBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddTransactionSetApiController(this IMvcBuilder MvcBuilder)
        {
            return MvcBuilder.AddApplicationPart(typeof(TransactionExtensions).Assembly);
        }
    }
}
