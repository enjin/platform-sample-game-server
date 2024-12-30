<?php

namespace App\GraphQL\Types\Global;

use Enjin\BlockchainTools\HexConverter;
use GraphQL\Bootstrapper\GraphQL\Types\Pagination\ConnectionInput;
use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use GraphQL\Bootstrapper\Traits\HasSelectFields;
use Illuminate\Pagination\Cursor;
use Illuminate\Pagination\CursorPaginator;
use Illuminate\Support\Arr;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class TokenType extends Type implements GraphQlType
{
    use HasSelectFields;

    /**
     * Get the type's attributes.
     */
    #[\Override]
    public function attributes(): array
    {
        return [
            'name' => 'Token',
            'description' => __('type.token.description'),
        ];
    }

    /**
     * Get the type's fields definition.
     */
    public function fields(): array
    {
        return [
            'collectionId' => [
                'type' => GraphQL::type('BigInt'),
                'description' => "The token's collection ID.",
            ],
            'tokenId' => [
                'type' => GraphQL::type('String'),
                'description' => "The token's ID.",
            ],
            'name' => [
                'type' => GraphQL::type('String'),
                'description' => "The token's name.",
                'resolve' => function ($token) {
                    return HexConverter::hexToString($token['name']);
                },
            ],
            'attributes' => [
                'type' => GraphQL::type('[Attribute]'),
                'description' => "The token's attributes.",
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
