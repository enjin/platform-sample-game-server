<?php

namespace App\GraphQL\Schemas\Primary\Queries;

use App\GraphQL\Query;
use App\GraphQL\Traits\InPrimarySchema;
use GraphQL\Bootstrapper\Interfaces\GraphQlQuery;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;

class GetUserQuery extends Query implements GraphQlQuery
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'GetUser',
        'description' => 'Get a user from their API token.',
    ];

    public function type(): Type
    {
        return GraphQL::type('User');
    }

    public function args(): array
    {
        return [];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo)
    {
        return $authUser;
    }
}
