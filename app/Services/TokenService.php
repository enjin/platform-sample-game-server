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

    public function transferToken() {}
}
