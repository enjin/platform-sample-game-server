<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class SignatureType extends Type implements GraphQlType
{
    /**
     * Get the type's attributes.
     */
    public function attributes(): array
    {
        return [
            'name' => 'WalletSignature',
            'description' => 'A wallet signature.',
        ];
    }

    /**
     * Get the type's fields definition.
     */
    public function fields(): array
    {
        return [
            'payload' => [
                'type' => GraphQL::type('String'),
                'description' => 'The signature payload.',
            ],
            'timestamp' => [
                'type' => GraphQL::type('Int'),
                'description' => 'The signature timestamp.',
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
