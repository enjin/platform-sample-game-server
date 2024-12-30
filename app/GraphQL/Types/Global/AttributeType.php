<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class AttributeType extends Type implements GraphQlType
{
    /**
     * Get the type's attributes.
     */
    public function attributes(): array
    {
        return [
            'name' => 'Attribute',
            'description' => 'An attribute.',
        ];
    }

    /**
     * Get the type's fields definition.
     */
    public function fields(): array
    {
        return [
            'key' => [
                'type' => GraphQL::type('String!'),
                'description' => 'The key of the attribute.',
            ],
            'value' => [
                'type' => GraphQL::type('String!'),
                'description' => 'The value of the attribute.',
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
