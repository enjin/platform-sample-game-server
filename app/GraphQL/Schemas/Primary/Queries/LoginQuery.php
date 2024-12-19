<?php

namespace App\GraphQL\Schemas\Primary\Queries;

use App\GraphQL\Query;
use App\GraphQL\Traits\InPrimarySchema;
use App\Services\UserService;
use Closure;
use GraphQL\Bootstrapper\Interfaces\GraphQlQuery;
use GraphQL\Bootstrapper\Interfaces\PublicGraphQlOperation;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;

class LoginQuery extends Query implements GraphQlQuery, PublicGraphQlOperation
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'Login',
        'description' => 'Login a user and generate an API token.',
    ];

    public function authorize($root, array $args, $ctx, ?ResolveInfo $resolveInfo = null, ?Closure $getSelectFields = null): bool
    {
        return true;
    }

    public function type(): Type
    {
        return GraphQL::type('String');
    }

    public function args(): array
    {
        return [
            'email' => [
                'type' => Type::nonNull(Type::string()),
            ],
            'password' => [
                'type' => Type::nonNull(Type::string()),
            ],
        ];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, UserService $userService)
    {
        return $userService->login($args['email'], $args['password']);
    }
}
