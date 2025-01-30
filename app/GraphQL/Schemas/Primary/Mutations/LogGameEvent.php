<?php

namespace App\GraphQL\Schemas\Primary\Mutations;

use App\Enums\GameEventType;
use App\GraphQL\Traits\InPrimarySchema;
use App\Services\TokenService;
use App\Services\WalletAccountService;
use Closure;
use GraphQL\Bootstrapper\Interfaces\GraphQlMutation;
use GraphQL\Type\Definition\ResolveInfo;
use GraphQL\Type\Definition\Type;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Mutation;

class LogGameEvent extends Mutation implements GraphQlMutation
{
    use InPrimarySchema;

    public function authorize($root, array $args, $ctx, ?ResolveInfo $resolveInfo = null, ?Closure $getSelectFields = null): bool
    {
        return isset($ctx, $args['signature'], $args['timestamp']) && $args['signature'] === hash_hmac('sha256', implode('|', [$ctx->uuid, $args['timestamp']]), config('app.key'));
    }

    protected $attributes = [
        'name' => 'LogGameEvent',
        'description' => 'Logs a game event with the server.',
    ];

    public function type(): Type
    {
        return GraphQL::type('Boolean');
    }

    public function args(): array
    {
        return [
            'eventType' => [
                'type' => GraphQL::type('GameEventType!'),
                'description' => 'The event type.',
            ],
            'data' => [
                'type' => GraphQL::type('String!'),
                'description' => 'The data for the event.',
            ],
            'signature' => [
                'type' => GraphQL::type('String!'),
                'description' => 'The signature for this request.',
            ],
            'timestamp' => [
                'type' => GraphQL::type('Int!'),
                'description' => 'The timestamp for this request.',
            ],
        ];
    }

    public function resolve($root, $args, $authUser, ResolveInfo $resolveInfo, TokenService $tokenService, WalletAccountService $walletAccountService)
    {
        if (! $walletAccount = $walletAccountService->getManagedWalletAccount($authUser->uuid, ['payload' => $args['signature'], 'timestamp' => $args['timestamp']])) {
            return false;
        }

        $data = json_decode(base64_decode($args['data']), true);

        ray($args, $data)->label('Args and Data');

        match ($args['eventType']) {
            GameEventType::ITEM_COLLECTED->name => $tokenService->mintToken($walletAccount['address'], ...$data),
            GameEventType::ITEM_MELTED->name => $tokenService->burnToken(...$data),
            GameEventType::ITEM_TRANSFERRED->name => $tokenService->transferToken(...$data),
            default => function () use ($data) {
                ray('Unknown event type', ...$data)->label('Event data');

                return false;
            },
        };

        return true;
    }
}
