<?php

return [

    /*
    |--------------------------------------------------------------------------
    | The Platform URL
    |--------------------------------------------------------------------------
    |
    | Here you may specify the platform URL that this server will connect to
    |
    */

    'url' => env('PLATFORM_URL'),

    /*
    |--------------------------------------------------------------------------
    | Authentication
    |--------------------------------------------------------------------------
    |
    | Below you may configure the platform's auth token.
    |
    */

    'auth_token' => env('PLATFORM_AUTH_TOKEN'),

    /*
    |--------------------------------------------------------------------------
    | Primary Schema
    |--------------------------------------------------------------------------
    |
    | Below you may configure the platform's primary schema name.
    |
    */

    'primary_schema' => env('PLATFORM_PRIMARY_SCHEMA'),

];
