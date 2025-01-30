<?php

namespace App\Services;

use App\Http\Clients\GraphQlClient;

class TokenService
{
    public function __construct(protected GraphQlClient $graphQlClient) {}

    public function getToken($collectionId, $tokenId): array
    {
        $tokenId = ['integer' => $tokenId];
        $token = $this->graphQlClient->graphQl('GetToken', collectionId: $collectionId, tokenId: $tokenId);

        return [
            'collectionId' => $collectionId,
            'tokenId' => $tokenId['integer'],
            'name' => $token['tokenMetadata']['name'],
            'attributes' => $token['attributes'],
        ];
    }

    public function getTokens() {}

    public function mintToken($recipient, $collectionId, $tokenId, $amount): bool
    {
        $tokenId = ['integer' => $tokenId];
        $params = [
            'tokenId' => $tokenId,
            'amount' => $amount,
        ];

        $this->graphQlClient->graphQl('MintToken', collectionId: $collectionId, params: $params, recipient: $recipient);

        return true;
    }

    public function burnToken($collectionId, $tokenId, $amount, $signingAccount): bool
    {
        $tokenId = ['integer' => $tokenId];
        $params = [
            'tokenId' => $tokenId,
            'amount' => $amount,
        ];

        $this->graphQlClient->graphQl('BurnToken', collectionId: $collectionId, params: $params, signingAccount: $signingAccount);

        return true;
    }

    public function transferToken($recipient, $collectionId, $tokenId, $amount, $signingAccount): bool
    {
        $tokenId = ['integer' => $tokenId];
        $params = [
            'tokenId' => $tokenId,
            'amount' => $amount,
        ];

        $result = $this->graphQlClient->graphQl('SimpleTransferToken', collectionId: $collectionId, recipient: $recipient, params: $params, signingAccount: $signingAccount);
        ray($result)->label('TransferToken');

        return true;
    }
}
