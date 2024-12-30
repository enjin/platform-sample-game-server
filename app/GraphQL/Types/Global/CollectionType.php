<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\GraphQL\Types\Pagination\ConnectionInput;
use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use GraphQL\Bootstrapper\Traits\HasSelectFields;
use Illuminate\Pagination\Cursor;
use Illuminate\Pagination\CursorPaginator;
use Illuminate\Support\Arr;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class CollectionType extends Type implements GraphQlType
{
    use HasSelectFields;

    /**
     * Get the type's attributes.
     */
    #[\Override]
    public function attributes(): array
    {
        return [
            'name' => 'Collection',
            'description' => __('type.collection.description'),
        ];
    }

    /**
     * Get the type's fields definition.
     */
    #[\Override]
    public function fields(): array
    {
        return [
            'maxTokenCount' => [
                'type' => GraphQL::type('Int'),
                'description' => __('enjin-platform::type.collection_type.field.maxTokenCount'),
            ],
            'maxTokenSupply' => [
                'type' => GraphQL::type('BigInt'),
                'description' => __('enjin-platform::type.collection_type.field.maxTokenSupply'),
            ],
            'attributes' => [
                'type' => GraphQL::type('[Attribute]'),
                'description' => __('enjin-platform::type.collection_type.field.attributes'),
            ],
            'tokens' => [
                'type' => GraphQL::type('[Token]'),
                'description' => __('enjin-platform::type.collection_type.field.tokens'),
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
