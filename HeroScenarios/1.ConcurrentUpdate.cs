﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace HeroScenarios
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    public class ConcurrentUpdate : BaseScenario
    {
        private const uint ConcurrencyRetires = 3;

        public async Task<TwoPersonGame> IncrementUser1ScoreAsync(
            string gameDay, 
            string gameId, 
            uint increment,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentNullException(nameof(gameId));
            if (string.IsNullOrWhiteSpace(gameDay)) throw new ArgumentNullException(nameof(gameDay));

            ItemResponse<TwoPersonGame> gameReadResponse = await this.container
                        .ReadItemAsync<TwoPersonGame>(gameDay, gameId, cancellationToken: cancellationToken);
            if (gameReadResponse.StatusCode == HttpStatusCode.NotFound)
            {
                TwoPersonGame newGame = new TwoPersonGame(gameDay, gameId);
                newGame.User1Score = increment;

                try
                {
                    return await this.container.CreateItemAsync<TwoPersonGame>(newGame, cancellationToken: cancellationToken);
                }
                // Concurrent write conflict
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    // Assume game never gets deleted :-)
                    gameReadResponse = await this.container.ReadItemAsync<TwoPersonGame>(gameDay, gameId, cancellationToken: cancellationToken);
                }
            }

            int retryCount = 0;

            while(true)
            {
                retryCount++;

                TwoPersonGame game = gameReadResponse;
                game.User1Score += increment;

                // Pre-condition
                CosmosItemRequestOptions options = new CosmosItemRequestOptions();
                options.IfMatchEtag = gameReadResponse.ETag;

                try
                {
                    ItemResponse<TwoPersonGame> gameReplaceResponse = await this.container
                        .UpsertItemAsync<TwoPersonGame>(game, options, cancellationToken: cancellationToken);
                    return gameReplaceResponse.Resource;
                }
                catch (CosmosException ex) 
                    when (retryCount < ConcurrentUpdate.ConcurrencyRetires         // Max retries
                          && ex.StatusCode == HttpStatusCode.PreconditionFailed) // Concurrent update
                {
                    // Retry by reading again
                    gameReadResponse = await this.container.ReadItemAsync<TwoPersonGame>(gameId, gameId, cancellationToken: cancellationToken);
                }
            }
        }
    }
}
