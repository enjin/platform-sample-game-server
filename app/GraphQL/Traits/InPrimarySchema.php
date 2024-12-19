<?php

namespace App\GraphQL\Traits;

trait InPrimarySchema
{
    public static function getSchemaName(): string
    {
        return 'primary';
    }
}
