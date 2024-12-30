<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use GraphQL\Bootstrapper\Traits\HasSelectFields;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class TokenAccountType extends Type implements GraphQlType
{
    use HasSelectFields;

    /**
     * Get the type's attributes.
     */
    #[\Override]
    public function attributes(): array
    {
        return [
            'name' => 'TokenAccount',
            'description' => __('type.token.description'),
        ];
    }

    /**
     * Get the type's fields definition.
     */
    public function fields(): array
    {
        return [
            'balance' => [
                'type' => GraphQL::type('BigInt'),
                'description' => "The token's balance.",
            ],
            'token' => [
                'type' => GraphQL::type('Token'),
                'description' => 'The token.',
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
