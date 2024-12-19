<?php

namespace App\Services;

use App\Models\User;
use Illuminate\Support\Arr;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

class UserService
{
    public function create(array $args, bool $sendEmailVerification = false): User
    {
        if ($user = User::where('email', '=', $args['email'])->first()) {
            return $user;
        }

        $inputData = array_merge(
            [
                'uuid' => Str::uuid()->toString(),
                'password' => Hash::make($args['password']),
            ],
            Arr::except($args, ['password'])
        );

        /** @var User $user */
        return User::create($inputData);
    }

    public function login(string $email, string $password)
    {
        $credentials = [
            'email' => $email,
            'password' => $password,
        ];

        if (! Auth::guard('web')->attempt($credentials)) {
            return 'unauthorized';
        }

        $user = User::where('email', '=', $email)->first();
        $user->tokens()->delete();

        return $user->createToken('sample-game', ['client:player'])->plainTextToken;
    }
}
