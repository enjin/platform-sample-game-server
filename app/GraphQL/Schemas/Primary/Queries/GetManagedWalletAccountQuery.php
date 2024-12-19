<?php

namespace App\GraphQL\Schemas\Primary\Queries;

use App\GraphQL\Query;
use App\GraphQL\Traits\InPrimarySchema;
use App\Services\WalletAccountService;
use GraphQL\Bootstrapper\Interfaces\GraphQlQuery;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;

class GetManagedWalletAccountQuery extends Query implements GraphQlQuery
{
    use InPrimarySchema;

    protected $attributes = [
        'name' => 'GetManagedWalletAccount',
        'description' => "Get a user's managed wallet.",
    ];

    public function type(): Type
    {
        return GraphQL::type('String');
    }

    public function args(): array
    {
        return [];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, WalletAccountService $walletAccountService)
    {
        return $walletAccountService->getManagedWalletAccount($authUser->uuid)['account']['address'];
    }
}
