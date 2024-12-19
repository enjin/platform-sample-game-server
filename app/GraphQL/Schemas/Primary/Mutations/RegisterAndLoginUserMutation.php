<?php

namespace App\GraphQL\Schemas\Primary\Mutations;

use App\GraphQL\Traits\InPrimarySchema;
use App\Services\UserService;
use Closure;
use GraphQL\Bootstrapper\Interfaces\GraphQlMutation;
use GraphQL\Bootstrapper\Interfaces\PublicGraphQlOperation;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Mutation;

class RegisterAndLoginUserMutation extends Mutation implements GraphQlMutation, PublicGraphQlOperation
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'RegisterAndLoginUser',
        'description' => 'Register a new user.',
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
                'description' => "The user's email.",
                'rules' => ['email', 'max:255'],
            ],
            'password' => [
                'type' => Type::nonNull(Type::string()),
                'description' => "The user's password.",
                'rules' => ['min:8'],
            ],
        ];
    }

    public function resolve($root, $args, $context, ResolveInfo $resolveInfo, UserService $userService)
    {
        $userService->create($args);

        return $userService->login($args['email'], $args['password']);
    }
}
