<?php

namespace App\GraphQL\Schemas\Primary\Mutations;

use App\GraphQL\Traits\InPrimarySchema;
use App\Services\WalletAccountService;
use GraphQL\Bootstrapper\Interfaces\GraphQlMutation;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Mutation;

class CreateManagedWalletAccountMutation extends Mutation implements GraphQlMutation
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'CreateManagedWalletAccount',
        'description' => 'Create a managed wallet account for the logged in user.',
    ];

    public function type() : Type
    {
        return GraphQL::type('Boolean');
    }

    public function args() : array
    {
        return [];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, WalletAccountService $walletAccountService)
    {
        if ($walletAccountService->getManagedWalletAccount($authUser->uuid)) {
            return false;
        }

        return $walletAccountService->createManagedWalletAccount($authUser->uuid);
    }
}
