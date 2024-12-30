<?php

namespace App\Services;

use App\Http\Clients\GraphQlClient;

class CollectionService
{
    public function __construct(protected GraphQlClient $graphQlClient) {}

    public function getCollections() {}

    public function getCollection($collectionId): array
    {
        $collection = $this->graphQlClient->graphQl('GetCollection', collectionId: $collectionId);

        $tokens = collect($collection['tokens']['edges'])->map(function ($edge) use ($collectionId) {
            $token = $edge['node'];

            return [
                'collectionId' => $collectionId,
                'tokenId' => $token['tokenId'],
                'name' => $token['tokenMetadata']['name'],
            ];
        });

        return [
            'maxTokenCount' => $collection['maxTokenCount'],
            'maxTokenSupply' => $collection['maxTokenSupply'],
            'attributes' => $collection['attributes'],
            'tokens' => $tokens,
        ];
    }
}
