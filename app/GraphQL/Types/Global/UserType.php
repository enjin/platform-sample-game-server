<?php

namespace App\GraphQL\Types\Global;

use GraphQL\Bootstrapper\Interfaces\GraphQlType;
use Rebing\GraphQL\Support\Facades\GraphQL;
use Rebing\GraphQL\Support\Type;

class UserType extends Type implements GraphQlType
{
    protected $attributes = [
        'name' => 'User',
        'description' => 'A user account.',
    ];

    public function fields(): array
    {
        return [
            'id' => [
                'type' => GraphQL::type('Int'),
                'description' => 'The id of the User.',
            ],
            'uuid' => [
                'type' => GraphQL::type('String'),
                'description' => "The user's UUID.",
            ],
            'email' => [
                'type' => GraphQL::type('String'),
                'description' => "The user's email address.",
            ],
            'isVerified' => [
                'type' => GraphQL::type('Boolean'),
                'description' => 'Check if the user has verified their email address.',
                'resolve' => function ($user) {
                    return isset($user->email_verified_at);
                },
            ],
            'updatedAt' => [
                'type' => GraphQL::type('String'),
                'resolve' => function ($user) {
                    return $user->updated_at->toIso8601String();
                },
            ],
        ];
    }

    public static function getSchemaName(): string
    {
        return '';
    }
}
