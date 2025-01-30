<?php

namespace App\Services;

use App\Http\Clients\GraphQlClient;
use Illuminate\Support\Carbon;

class WalletAccountService
{
    public function __construct(protected GraphQlClient $graphQlClient) {}

    public function getWalletAccount() {}

    public function getWalletAccounts() {}

    public function getManagedWalletAccount(string $externalId, ?array $signature = null)
    {
        $wallet = $this->graphQlClient->graphQl('GetManagedWallet', externalId: $externalId);

        if (! $wallet) {
            return null;
        }

        $tokenAccounts = collect($wallet['tokenAccounts']['edges'] ?? [])->map(function ($edge) {
            $tokenAccount = $edge['node'];

            return [
                'balance' => $tokenAccount['balance'],
                'token' => [
                    'collectionId' => $tokenAccount['token']['collection']['collectionId'],
                    'tokenId' => $tokenAccount['token']['tokenId'],
                    'name' => $tokenAccount['token']['tokenMetadata']['name'],
                ],
            ];
        });

        $timestamp = Carbon::now()->timestamp;

        return [
            'address' => $wallet['account']['address'],
            'publicKey' => $wallet['account']['publicKey'],
            'tokens' => $tokenAccounts,
            'signature' => $signature ?? [
                'payload' => hash_hmac('sha256', implode('|', [$externalId, $timestamp]), config('app.key')),
                'timestamp' => $timestamp,
            ],
        ];
    }

    public function createManagedWalletAccount(string $externalId)
    {
        return $this->graphQlClient->graphQl('CreateManagedWallet', externalId: $externalId);
    }
}
