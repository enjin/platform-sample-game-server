<?php

namespace App\GraphQL\Schemas\Primary\Queries;

use App\GraphQL\Query;
use App\GraphQL\Traits\InPrimarySchema;
use App\Rules\MinBigInt;
use App\Services\CollectionService;
use GraphQL\Bootstrapper\Interfaces\GraphQlQuery;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;

class GetCollectionQuery extends Query implements GraphQlQuery
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'GetCollection',
        'description' => 'Get a collection.',
    ];

    public function type(): Type
    {
        return GraphQL::type('Collection');
    }

    public function args(): array
    {
        return [
            'collectionId' => [
                'type' => GraphQL::type('BigInt!'),
                'description' => 'The collection ID',
                'rules' => [
                    new MinBigInt(2000),
                ],
            ],
        ];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, CollectionService $collectionService)
    {
        return $collectionService->getCollection($args['collectionId']);
    }
}
