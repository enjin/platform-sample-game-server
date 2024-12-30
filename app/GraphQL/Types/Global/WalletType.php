<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class WalletType extends Type implements GraphQlType
{
    /**
     * Get the type's attributes.
     */
    public function attributes(): array
    {
        return [
            'name' => 'Wallet',
            'description' => 'A wallet.',
        ];
    }

    /**
     * Get the type's fields definition.
     */
    public function fields(): array
    {
        return [
            'address' => [
                'type' => GraphQL::type('String'),
                'description' => 'The wallet address.',
            ],
            'tokens' => [
                'type' => GraphQL::type('[TokenAccount]'),
                'description' => 'The wallet tokens.',
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
