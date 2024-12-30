<?php

namespace App\GraphQL\Schemas\Primary\Queries;

use App\GraphQL\Query;
use App\GraphQL\Traits\InPrimarySchema;
use App\Rules\MinBigInt;
use App\Services\CollectionService;
use App\Services\TokenService;
use GraphQL\Bootstrapper\Interfaces\GraphQlQuery;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;

class GetTokenQuery extends Query implements GraphQlQuery
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'GetToken',
        'description' => 'Get a token.',
    ];

    public function type(): Type
    {
        return GraphQL::type('Token');
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
            'tokenId' => [
                'type' => GraphQL::type('BigInt!'),
                'description' => 'The token ID',
                'rules' => [
                    new MinBigInt(0),
                ],
            ],
        ];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, TokenService $tokenService)
    {
        return $tokenService->getToken($args['collectionId'], $args['tokenId']);
    }
}
