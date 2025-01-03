<?php

namespace App\GraphQL\Types\Global;

use App\Enums\GameEventType;
use GraphQL\Bootstrapper\Interfaces\GraphQlEnum;
use Rebing\GraphQL\Support\EnumType;

class GameEventTypeEnum extends EnumType implements GraphQlEnum
{
    /**
     * Get the enum's attributes.
     */
    #[\Override]
    public function attributes(): array
    {
        return [
            'name' => 'GameEventType',
            'values' => GameEventType::caseNamesAsArray(),
            'description' => 'The game event type.',
        ];
    }
}
