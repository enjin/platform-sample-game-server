<?php

namespace App\Enums;

use GraphQL\Bootstrapper\Traits\EnumExtensions;

enum GameEventType: string
{
    use EnumExtensions;

    case ITEM_COLLECTED = 'itemCollected';
}
